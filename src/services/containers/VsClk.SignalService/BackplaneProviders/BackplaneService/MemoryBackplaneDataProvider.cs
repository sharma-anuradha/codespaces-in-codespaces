using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// Implements IBackplaneServiceDataProvider interface using memory storage types
    /// </summary>
    public class MemoryBackplaneDataProvider : IBackplaneServiceDataProvider
    {
        /// <summary>
        /// Store a dictionary with contact data info
        /// </summary>
        private ConcurrentDictionary<string, ContactDataInfoHolder> Contacts { get; } = new ConcurrentDictionary<string, ContactDataInfoHolder>();

        private readonly IDataFormatProvider formatProvider;

        public MemoryBackplaneDataProvider(ILogger<MemoryBackplaneDataProvider> logger, IDataFormatProvider formatProvider = null)
        {
            Logger = Requires.NotNull(logger, nameof(logger));
            this.formatProvider = formatProvider;
        }

        private ILogger Logger { get; }

        /// <summary>
        /// Dictionary of email -> contactId
        /// </summary>
        private ConcurrentDictionary<string, string> Emails { get; } = new ConcurrentDictionary<string, string>();

        public string[] ActiveServices { get; set; } = Array.Empty<string>();

        public Task<bool> ContainsContactAsync(string contactId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Contacts.ContainsKey(contactId));
        }

        public Task UpdateContactDataInfoAsync(string contactId,ContactDataInfo contactDataInfo, CancellationToken cancellationToken)
        {
            var contactdDataInfoHolder = new ContactDataInfoHolder(contactDataInfo);
            Contacts.AddOrUpdate(
                contactId,
                (k) => contactdDataInfoHolder,
                (k, v) => contactdDataInfoHolder);

            LogUpdateContact(contactId, nameof(UpdateContactDataInfoAsync));
            return Task.CompletedTask;
        }

        public Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken)
        {
            if (Contacts.TryGetValue(contactId, out var contactdDataInfoHolder))
            {
                return Task.FromResult(contactdDataInfoHolder.Data);
            }

            return Task.FromResult<ContactDataInfo>(null);
        }

        public Task<ContactDataInfo> UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken)
        {
            var result = Contacts.AddOrUpdate(
                contactDataChanged.ContactId,
                (k) =>
                {
                    var contactdDataInfoHolder = new ContactDataInfoHolder();
                    contactdDataInfoHolder.Update(contactDataChanged, null);
                    return contactdDataInfoHolder;
                }, (k, contactdDataInfoHolder) =>
                {
                    contactdDataInfoHolder.Update(contactDataChanged, ActiveServices);
                    return contactdDataInfoHolder;
                });

            if (contactDataChanged.Data.TryGetValue(ContactProperties.Email, out var pv) && !string.IsNullOrEmpty(pv.Value?.ToString()))
            {
                Emails.TryAdd(pv.Value?.ToString(), contactDataChanged.ContactId);
            }

            LogUpdateContact(contactDataChanged.ContactId, nameof(UpdateContactAsync));
            return Task.FromResult(result.Data);
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
                    matchingResult[contactId] = contactdDataInfoHolder.Data;
                    results[index] = matchingResult;
                }
            }

            return Task.FromResult(results);
        }

        private string Format(string format, params object[] args)
        {
            return string.Format(this.formatProvider, format, args);
        }

        private void LogUpdateContact(string contactId, string method)
        {
            Logger.LogMethodScope(LogLevel.Debug, $"contactId:{Format("{0:T}", contactId)} total:{Contacts.Count}", method);
        }

        /// <summary>
        /// Thread safe to hold a ContactDataInfo structure
        /// </summary>
        private class ContactDataInfoHolder
        {
            private readonly object lock_ = new object();
            private readonly ContactDataInfo contactDataInfo;

            public ContactDataInfoHolder()
            {
                this.contactDataInfo = new Dictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>();
            }

            public ContactDataInfoHolder(ContactDataInfo contactDataInfo)
            {
                // ensure we clone the structure we hold
                this.contactDataInfo = contactDataInfo.Clone();
            }

            public void Update(ContactDataChanged<ConnectionProperties> contactDataChanged, string[] activeServices)
            {
                lock(this.lock_)
                {
                    this.contactDataInfo.UpdateConnectionProperties(contactDataChanged);
                    if (activeServices != null)
                    {
                        // Note: next block will remove 'stale' service entries
                        foreach (var serviceId in this.contactDataInfo.Keys.Where(serviceId => !activeServices.Contains(serviceId)).ToArray())
                        {
                            contactDataInfo.Remove(serviceId);
                        }
                    }
                }
            }

            public ContactDataInfo Data
            {
                get
                {
                    lock (this.lock_)
                    {
                        return this.contactDataInfo.Clone();
                    }
                }
            }
        }
    }
}
