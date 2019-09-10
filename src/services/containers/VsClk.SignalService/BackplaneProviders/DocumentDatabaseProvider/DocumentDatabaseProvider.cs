using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService
{
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// Base class to all our backplane providers that are based on a document database 
    /// </summary>
    public abstract class DocumentDatabaseProvider : IAsyncDisposable, IBackplaneProvider
    {
        private const int StaleServiceSeconds = 120;

        // Logger method scopes
        private const string MethodSendMessage = "DocumentDatabaseProvider.SendMessageAsync";
        private const string MethodUpdateContact = "DocumentDatabaseProvider.UpdateContact";
        private const string MethodOnServiceDocumentsChanged = "DocumentDatabaseProvider.OnServiceDocumentsChanged";
        private const string MethodOnContactDocumentsChanged = "DocumentDatabaseProvider.OnContactDocumentsChanged";
        private const string MethodOnMessageDocumentsChanged = "DocumentDatabaseProvider.OnMessageDocumentsChanged";

        private readonly IFormatProvider formatProvider;
        private HashSet<string> activeServices;

        protected DocumentDatabaseProvider(ILogger logger, IFormatProvider formatProvider)
        {
            Logger = Requires.NotNull(logger, nameof(logger));
            this.formatProvider = formatProvider;
        }

        protected ILogger Logger { get; }

        #region IAsyncDisposable

        public async Task DisposeAsync()
        {
            Logger.LogDebug($"StorageBackplaneProvider.Dispose");

            await DisposeInternalAsync();
        }

        #endregion

        #region IBackplaneProvider

        public OnContactChangedAsync ContactChangedAsync { get; set; }

        public OnMessageReceivedAsync MessageReceivedAsync { get; set; }

        public virtual int Priority => 0;

        public async Task UpdateMetricsAsync(string serviceId, object serviceInfo, PresenceServiceMetrics metrics, CancellationToken cancellationToken)
        {
            var serviceDocument = new ServiceDocument()
            {
                Id = serviceId,
                ServiceInfo = serviceInfo,
                Metrics = metrics,
                LastUpdate = DateTime.UtcNow
            };

            await UpsertServiceDocumentAsync(serviceDocument, cancellationToken);
        }

        public async Task<Dictionary<string, ContactDataInfo>> GetContactsDataAsync(Dictionary<string, object> matchProperties, CancellationToken cancellationToken)
        {
            var emailPropertyValue = matchProperties.TryGetProperty<string>(Properties.Email)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(emailPropertyValue))
            {
                return new Dictionary<string, ContactDataInfo>();
            }

            var matchContacts = await GetContactsDataByEmailAsync(emailPropertyValue, cancellationToken);
            return matchContacts.ToDictionary(d => d.Id, d => ToContactData(d));
        }

        public async Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken)
        {
            var contactDataCoument = (await GetContactDataDocumentAsync(contactId, cancellationToken)).Item1;
            return contactDataCoument != null ? ToContactData(contactDataCoument) : null;
        }

        public async Task SendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
        {
            using (Logger.BeginSingleScope(
                (LoggerScopeHelpers.MethodScope, MethodSendMessage)))
            {
                Logger.LogDebug($"contactId:{ToTraceText(messageData.FromContact.Id)} targetContactId:{ToTraceText(messageData.TargetContact.Id)}");
            }

            var messageDocument = new MessageDocument()
            {
                Id = messageData.ChangeId,
                ContactId = messageData.FromContact.Id,
                TargetContactId = messageData.TargetContact.Id,
                TargetConnectionId = messageData.TargetContact.ConnectionId,
                Type = messageData.Type,
                Body = messageData.Body,
                SourceId = sourceId,
                LastUpdate = DateTime.UtcNow
            };

            await InsertMessageDocumentAsync(messageDocument, cancellationToken);
        }

        public async Task UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken)
        {
            ContactDataInfo contactDataInfo;
            var contactDataDocumentInfo = await GetContactDataDocumentAsync(contactDataChanged.ContactId, cancellationToken);
            if (contactDataDocumentInfo.Item1 == null)
            {
                contactDataInfo = new Dictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>();
            }
            else
            {
                contactDataInfo = ToContactData(contactDataDocumentInfo.Item1);
            }

            contactDataInfo.UpdateConnectionProperties(contactDataChanged);

            var contactDataDocument = new ContactDataDocument()
            {
                Id = contactDataChanged.ContactId,
                ChangeId = contactDataChanged.ChangeId,
                ServiceId = contactDataChanged.ServiceId,
                ConnectionId = contactDataChanged.ConnectionId,
                UpdateType = contactDataChanged.Type,
                Properties = contactDataChanged.Data.Keys.ToArray(),
                Email = contactDataInfo.GetAggregatedProperties().TryGetProperty<string>(Properties.Email)?.ToLowerInvariant() ?? contactDataDocumentInfo.Item1?.Email,
                ServiceConnections = JObject.FromObject(contactDataInfo),
                LastUpdate = DateTime.UtcNow
            };

            var requestLatency = await UpsertContactDocumentAsync(contactDataDocument, cancellationToken);


            using (Logger.BeginScope(
                (LoggerScopeHelpers.MethodScope, MethodUpdateContact),
                (LoggerScopeHelpers.MethodPerfScope, (requestLatency + contactDataDocumentInfo.Item2).Milliseconds)))
            {
                Logger.LogDebug($"contactId:{ToTraceText(contactDataChanged.ContactId)}");
            }
        }

        #endregion

        #region abstract Methods
        
        protected abstract Task DisposeInternalAsync();
        protected abstract Task<TimeSpan> UpsertContactDocumentAsync(ContactDataDocument contactDocument, CancellationToken cancellationToken);
        protected abstract Task InsertMessageDocumentAsync(MessageDocument messageDocument, CancellationToken cancellationToken);
        protected abstract Task UpsertServiceDocumentAsync(ServiceDocument serviceDocument, CancellationToken cancellationToken);
        protected abstract Task<List<ContactDataDocument>> GetContactsDataByEmailAsync(string email, CancellationToken cancellationToken);
        protected abstract Task<(ContactDataDocument, TimeSpan)> GetContactDataDocumentAsync(string contactId, CancellationToken cancellationToken);
        protected abstract Task<List<ServiceDocument>> GetServiceDocuments(CancellationToken cancellationToken);
        protected abstract Task DeleteServiceDocumentById(string serviceId, CancellationToken cancellationToken);

        #endregion

        protected async Task InitializeServiceIdAsync(string serviceId)
        {
            await UpdateMetricsAsync(serviceId, null, default, CancellationToken.None);
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
                        var contact = await doc.ReadAsAsync<ContactDataDocument>();
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

            using (Logger.BeginScope(
                (LoggerScopeHelpers.MethodScope, MethodOnContactDocumentsChanged),
                (LoggerScopeHelpers.MethodPerfScope, sw.ElapsedMilliseconds)))
            {
                Logger.LogDebug($"contactIds:{string.Join(",", docs.Select(d => ToTraceText(d.Id)))}");
            }

        }

        protected async Task OnMessageDocumentsChangedAsync(IReadOnlyList<IDocument> docs)
        {
            using (Logger.BeginSingleScope(
                (LoggerScopeHelpers.MethodScope, MethodOnMessageDocumentsChanged)))
            {
                Logger.LogDebug($"messageIds:{string.Join(",", docs.Select(d => d.Id))}");
            }

            if (MessageReceivedAsync != null)
            {
                foreach (var doc in docs)
                {
                    try
                    {
                        var message = await doc.ReadAsAsync<MessageDocument>();
                        await MessageReceivedAsync(
                            message.SourceId,
                            new MessageData(
                                message.Id,
                                new ContactReference(message.ContactId, null),
                                new ContactReference(message.TargetContactId, message.TargetConnectionId),
                                message.Type,
                                JToken.FromObject(message.Body)),
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
            var utcNow = DateTime.UtcNow;
            var nonStaleServices = new HashSet<string>(allServices.Where(i => (utcNow - i.LastUpdate).TotalSeconds < StaleServiceSeconds).Select(d => d.Id));

            // next block will delete stale documents
            foreach (var doc in allServices)
            {
                if (!nonStaleServices.Contains(doc.Id))
                {
                    try
                    {
                        await DeleteServiceDocumentById(doc.Id, cancellationToken);
                    }
                    catch { }
                }
            }

            // update
            this.activeServices = nonStaleServices;
        }

        private ContactDataInfo ToContactData(ContactDataDocument contactDataDocument)
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

        protected interface IDocument
        {
            string Id { get; }
            Task<T> ReadAsAsync<T>();
        }
    }
}
