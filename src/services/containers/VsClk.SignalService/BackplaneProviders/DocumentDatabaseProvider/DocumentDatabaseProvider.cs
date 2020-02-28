// <copyright file="DocumentDatabaseProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;
using Newtonsoft.Json.Linq;
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;
using ContactDataInfo = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>>;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Base class to all our backplane providers that are based on a document database
    /// </summary>
    public abstract class DocumentDatabaseProvider : VisualStudio.Threading.IAsyncDisposable, IContactBackplaneProvider
    {
        // Logger method scopes
        private const string MethodLoadActiveServices = "DocumentDatabaseProvider.LoadActiveServices";
        private const string MethodOnServiceDocumentsChanged = "DocumentDatabaseProvider.OnServiceDocumentsChanged";

        private readonly IFormatProvider formatProvider;
        private HashSet<string> activeServices;

        protected DocumentDatabaseProvider(ILogger logger, IFormatProvider formatProvider)
        {
            Logger = Requires.NotNull(logger, nameof(logger));
            this.formatProvider = formatProvider;
        }

        public OnContactChangedAsync ContactChangedAsync { get; set; }

        public OnMessageReceivedAsync MessageReceivedAsync { get; set; }

        protected ILogger Logger { get; }

        public async Task DisposeAsync()
        {
            Logger.LogDebug($"DocumentDatabaseProvider.Dispose");

            await DisposeInternalAsync();
        }

        public async Task UpdateMetricsAsync((string ServiceId, string Stamp) serviceInfo, ContactServiceMetrics metrics, CancellationToken cancellationToken)
        {
            var serviceDocument = new ServiceDocument()
            {
                Id = serviceInfo.ServiceId,
                Stamp = serviceInfo.Stamp,
                Metrics = metrics,
                LastUpdate = DateTime.UtcNow,
            };

            await UpsertServiceDocumentAsync(serviceDocument, cancellationToken);
        }

        public async Task<Dictionary<string, ContactDataInfo>[]> GetContactsDataAsync(Dictionary<string, object>[] matchProperties, CancellationToken cancellationToken)
        {
            var emailResults = new List<(int, string)>();
            int next = 0;

            Dictionary<string, ContactDataInfo>[] results = matchProperties.Select(item =>
            {
                var emailPropertyValue = item.TryGetProperty<string>(ContactProperties.Email)?.ToLowerInvariant();
                if (string.IsNullOrEmpty(emailPropertyValue))
                {
                    return new Dictionary<string, ContactDataInfo>();
                }

                emailResults.Add((next++, emailPropertyValue));

                // result will come later
                return null;
            }).ToArray();

            var matchContacts = await GetContactsDataByEmailAsync(emailResults.Select(i => i.Item2).ToArray(), cancellationToken);
            if (matchContacts?.Length == emailResults.Count)
            {
                for (next = 0; next < matchContacts.Length; ++next)
                {
                    results[emailResults[next].Item1] = matchContacts[next].ToDictionary(d => d.Id, d => ToContactData(d));
                }
            }

            return results;
        }

        public async Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken)
        {
            var contactDataCoument = await GetContactDataDocumentAsync(contactId, cancellationToken);
            return contactDataCoument != null ? ToContactData(contactDataCoument) : null;
        }

        public async Task SendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
        {
            var messageDocument = new MessageDocument()
            {
                Id = messageData.ChangeId,
                ContactId = messageData.FromContact.Id,
                TargetContactId = messageData.TargetContact.Id,
                TargetConnectionId = messageData.TargetContact.ConnectionId,
                Type = messageData.Type,
                Body = messageData.Body,
                SourceId = sourceId,
                LastUpdate = DateTime.UtcNow,
            };

            await InsertMessageDocumentAsync(messageDocument, cancellationToken);
        }

        public async Task<ContactDataInfo> UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken)
        {
            ContactDataInfo contactDataInfo;
            var contactDataDocument = await GetContactDataDocumentAsync(contactDataChanged.ContactId, cancellationToken);
            if (contactDataDocument == null)
            {
                contactDataInfo = new Dictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>();
            }
            else
            {
                contactDataInfo = ToContactData(contactDataDocument);
            }

            contactDataInfo.UpdateConnectionProperties(contactDataChanged);

            var updateContactDataDocument = new ContactDocument()
            {
                Id = contactDataChanged.ContactId,
                ChangeId = contactDataChanged.ChangeId,
                ServiceId = contactDataChanged.ServiceId,
                ConnectionId = contactDataChanged.ConnectionId,
                UpdateType = contactDataChanged.ChangeType,
                Properties = contactDataChanged.Data.Keys.ToArray(),
                Email = contactDataInfo.GetAggregatedProperties().TryGetProperty<string>(ContactProperties.Email)?.ToLowerInvariant() ?? contactDataDocument?.Email,
                ServiceConnections = JObject.FromObject(contactDataInfo),
                LastUpdate = DateTime.UtcNow,
            };

            await UpsertContactDocumentAsync(updateContactDataDocument, cancellationToken);
            return contactDataInfo;
        }

        public async Task DisposeDataChangesAsync(DataChanged[] dataChanges, CancellationToken cancellationToken)
        {
            var allMessageDataChanges = dataChanges.OfType<MessageData>().ToArray();

            if (allMessageDataChanges.Length > 0)
            {
                await DeleteMessageDocumentByIds(allMessageDataChanges.Select(i => i.ChangeId).ToArray(), cancellationToken);
            }
        }

        public virtual bool HandleException(string methodName, Exception error) => false;

        protected abstract Task DisposeInternalAsync();

        protected abstract Task UpsertContactDocumentAsync(ContactDocument contactDocument, CancellationToken cancellationToken);

        protected abstract Task InsertMessageDocumentAsync(MessageDocument messageDocument, CancellationToken cancellationToken);

        protected abstract Task UpsertServiceDocumentAsync(ServiceDocument serviceDocument, CancellationToken cancellationToken);

        protected abstract Task<List<ContactDocument>[]> GetContactsDataByEmailAsync(string[] emails, CancellationToken cancellationToken);

        protected abstract Task<ContactDocument> GetContactDataDocumentAsync(string contactId, CancellationToken cancellationToken);

        protected abstract Task<List<ServiceDocument>> GetServiceDocuments(CancellationToken cancellationToken);

        protected abstract Task DeleteServiceDocumentById(string serviceId, CancellationToken cancellationToken);

        protected abstract Task DeleteMessageDocumentByIds(string[] changeIds, CancellationToken cancellationToken);

        protected async Task InitializeServiceIdAsync((string ServiceId, string Stamp) serviceInfo)
        {
            await UpdateMetricsAsync(serviceInfo, default, CancellationToken.None);

            // load initial services
            await LoadActiveServicesAsync(default);
        }

        protected async Task OnServiceDocumentsChangedAsync(IReadOnlyList<IDocument> docs)
        {
            using (Logger.BeginSingleScope(
                (LoggerScopeHelpers.MethodScope, MethodOnServiceDocumentsChanged)))
            {
                Logger.LogDebug($"servicesIds:{string.Join(",", docs.Select(d => d.Id))}");
            }

            await LoadActiveServicesAsync(default(CancellationToken));
        }

        protected async Task OnContactDocumentsChangedAsync(IReadOnlyList<IDocument> docs)
        {
            var sw = new System.Diagnostics.Stopwatch();

            if (ContactChangedAsync != null)
            {
                foreach (var doc in docs)
                {
                    try
                    {
                        var contact = await doc.ReadAsAsync<ContactDocument>();
                        var contactDataInfoChanged = new ContactDataChanged<ContactDataInfo>(
                            contact.ChangeId,
                            contact.ServiceId,
                            contact.ConnectionId,
                            contact.Id,
                            contact.UpdateType,
                            ToContactData(contact));

                        await ContactChangedAsync(contactDataInfoChanged, contact.Properties ?? Array.Empty<string>(), default(CancellationToken));
                    }
                    catch (Exception error)
                    {
                        Logger.LogError(error, $"Failed when processing contact document:{doc.Id}");
                    }
                }
            }
        }

        protected async Task OnMessageDocumentsChangedAsync(IReadOnlyList<IDocument> docs)
        {
            if (MessageReceivedAsync != null)
            {
                foreach (var doc in docs)
                {
                    try
                    {
                        var messageDoc = await doc.ReadAsAsync<MessageDocument>();
                        await MessageReceivedAsync(
                            messageDoc.SourceId,
                            new MessageData(
                            messageDoc.Id,
                            new ContactReference(messageDoc.ContactId, null),
                            new ContactReference(messageDoc.TargetContactId, messageDoc.TargetConnectionId),
                            messageDoc.Type,
                            NewtonsoftHelpers.ToRawObject(messageDoc.Body)),
                            default(CancellationToken));
                    }
                    catch (Exception error)
                    {
                        Logger.LogError(error, $"Failed when processing message document:{doc.Id}");
                    }
                }
            }
        }

        private async Task LoadActiveServicesAsync(CancellationToken cancellationToken)
        {
            var allServices = await GetServiceDocuments(cancellationToken);
            Logger.LogMethodScope(
                LogLevel.Debug,
                $"services:{string.Join(",", allServices.Select(s => $"[{s.Id}-{s.Stamp}]"))}",
                MethodLoadActiveServices);

            var utcNow = DateTime.UtcNow;
            var nonStaleServices = new HashSet<string>(allServices.Where(i => (utcNow - i.LastUpdate).TotalSeconds < BackplaneManagerConst.StaleServiceSeconds).Select(d => d.Id));

            // next block will delete stale documents
            foreach (var doc in allServices)
            {
                if (!nonStaleServices.Contains(doc.Id))
                {
                    try
                    {
                        Logger.LogDebug($"Delete stale service id:{doc.Id}");
                        await DeleteServiceDocumentById(doc.Id, cancellationToken);
                    }
                    catch
                    {
                    }
                }
            }

            // update
            this.activeServices = nonStaleServices;
        }

        private ContactDataInfo ToContactData(ContactDocument contactDataDocument)
        {
            var contactDataInfo = ((JObject)contactDataDocument.ServiceConnections).ToObject<ContactDataInfo>();

            // Note: next block will remove 'stale' service entries
            foreach (var serviceId in contactDataInfo.Keys.Where(serviceId => !this.activeServices.Contains(serviceId)).ToArray())
            {
                contactDataInfo.Remove(serviceId);
            }

            return contactDataInfo;
        }

        private string ToTraceText(string s)
        {
            return string.Format(this.formatProvider, "{0:T}", s);
        }

#pragma warning disable SA1201 // Elements should appear in the correct order
        protected interface IDocument
#pragma warning restore SA1201 // Elements should appear in the correct order
        {
            string Id { get; }

            Task<T> ReadAsAsync<T>();
        }
    }
}
