// <copyright file="MemoryBackplaneDataProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Common;
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;
using ContactDataInfo = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>>;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// Implements IBackplaneServiceDataProvider interface using memory storage types.
    /// </summary>
    public class MemoryBackplaneDataProvider : IBackplaneServiceDataProvider
    {
        private readonly IDataFormatProvider formatProvider;

        public MemoryBackplaneDataProvider(ILogger<MemoryBackplaneDataProvider> logger, IDataFormatProvider formatProvider = null)
        {
            Logger = Requires.NotNull(logger, nameof(logger));
            this.formatProvider = formatProvider;
        }

        public int TotalContacts => Contacts.Count;

        public int TotalConnections => Contacts.Values.Sum(item => item.ConnectionsCount);

        /// <summary>
        /// Gets the dictionary with contact data info in json format.
        /// </summary>
        private ConcurrentDictionary<string, ContactDataInfoHolderBase> Contacts { get; } = new ConcurrentDictionary<string, ContactDataInfoHolderBase>();

        private ILogger Logger { get; }

        /// <summary>
        /// Gets the Dictionary of email -> contactId.
        /// </summary>
        private ConcurrentDictionary<string, string> Emails { get; } = new ConcurrentDictionary<string, string>();

        public Task<bool> ContainsContactAsync(string contactId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Contacts.ContainsKey(contactId));
        }

        public Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken)
        {
            if (Contacts.TryGetValue(contactId, out var contactdDataInfoHolder))
            {
                return Task.FromResult(contactdDataInfoHolder.GetAggregatedDataInfo());
            }

            return Task.FromResult<ContactDataInfo>(null);
        }

        public Task<ContactDataInfo> UpdateRemoteDataInfoAsync(
            string contactId,
            ContactDataInfo remoteDataInfo,
            CancellationToken cancellationToken)
        {
            var result = ContactsAddOrUpdate(
                contactId,
                (contactDataInfoHolder, isCreate) => contactDataInfoHolder.UpdateRemote(remoteDataInfo));

            return Task.FromResult(result.GetAggregatedDataInfo());
        }

        public Task<(ContactDataInfo NewValue, ContactDataInfo OldValue)> UpdateLocalDataChangedAsync<T>(
                ContactDataChangedRef<T> localDataChangedRef,
                string[] localServices,
                CancellationToken cancellationToken)
            where T : class
        {
            bool isConnectionProperties = typeof(T) == typeof(ConnectionProperties);

            ContactDataInfo oldValue = null;
            var result = ContactsAddOrUpdate(
                localDataChangedRef.DataChanged.ContactId,
                (contactDataInfoHolder, isCreate) =>
                {
                    if (!isCreate)
                    {
                        oldValue = contactDataInfoHolder.GetAggregatedDataInfo();
                    }

                    if (localDataChangedRef.IsConnectionProperties)
                    {
                        contactDataInfoHolder.UpdateLocal(localDataChangedRef.ConnectionProperties, localServices);
                    }
                    else
                    {
                        contactDataInfoHolder.UpdateLocal(localDataChangedRef.ContactDataInfo.Data, localServices);
                    }
                });

            HandleEmailIndexing(localDataChangedRef.DataChanged.ContactId, localDataChangedRef.ConnectionProperties.Data);
            LogUpdateContact(localDataChangedRef.DataChanged.ContactId, nameof(UpdateLocalDataChangedAsync));
            return Task.FromResult((result.GetAggregatedDataInfo(), oldValue));
        }

        public Task UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, ContactDataInfo>[]> GetContactsDataAsync(Dictionary<string, object>[] allMatchProperties, CancellationToken cancellationToken)
        {
            var results = new Dictionary<string, ContactDataInfo>[allMatchProperties.Length];

            for (var index = 0; index < allMatchProperties.Length; ++index)
            {
                var matchProperties = allMatchProperties[index];
                var emailPropertyValue = matchProperties.TryGetProperty<string>(ContactProperties.Email)?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(emailPropertyValue) &&
                    Emails.TryGetValue(emailPropertyValue, out var contactId) &&
                    Contacts.TryGetValue(contactId, out var contactdDataInfoHolder))
                {
                    var matchingResult = new Dictionary<string, ContactDataInfo>();
                    matchingResult[contactId] = contactdDataInfoHolder.GetAggregatedDataInfo();
                    results[index] = matchingResult;
                }
            }

            return Task.FromResult(results);
        }

        private void HandleEmailIndexing(string contactId, ConnectionProperties connectionProperties)
        {
            if (connectionProperties.TryGetValue(ContactProperties.Email, out var pv) && !string.IsNullOrEmpty(pv.Value?.ToString()))
            {
                var email = pv.Value.ToString();
                Logger.LogDebug($"email:{string.Format(this.formatProvider, "{0:E}", email)} contact id:{string.Format(this.formatProvider, "{0:T}", contactId)}");
                Emails.TryAdd(email, contactId);
            }
        }

        private ContactDataInfoHolderBase ContactsAddOrUpdate(string contactId, Action<ContactDataInfoHolderBase, bool> callback)
        {
            return Contacts.AddOrUpdate(
                contactId,
                (k) =>
                {
                    var contactDataInfoHolder = new MessagePackDataInfoHolder();
                    callback(contactDataInfoHolder, true);
                    return contactDataInfoHolder;
                }, (k, contactDataInfoHolder) =>
                {
                    callback(contactDataInfoHolder, false);
                    return contactDataInfoHolder;
                });
        }

        private string Format(string format, params object[] args)
        {
            return string.Format(this.formatProvider, format, args);
        }

        private void LogUpdateContact(string contactId, string method)
        {
            Logger.LogMethodScope(LogLevel.Debug, $"contactId:{Format("{0:T}", contactId)} total:{Contacts.Count}", method);
        }
    }
}
