using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// The non Hub Service class instance that manage all the registered contacts
    /// </summary>
    public class PresenceService : HubService<PresenceServiceHub>, IAsyncDisposable
    {
        private readonly List<IBackplaneProvider> backplaneProviders = new List<IBackplaneProvider>();

        private readonly ConcurrentDictionary<string, StubContact> stubContacts = new ConcurrentDictionary<string, StubContact>();

        /// <summary>
        /// Map of self contact id <-> stub contact
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentHashSet<StubContact>> resolvedContacts = new ConcurrentDictionary<string, ConcurrentHashSet<StubContact>>();

        public PresenceService(
            PresenceServiceOptions options,
            IEnumerable<IHubContextHost> hubContextHosts,
            ILogger<PresenceService> logger)
            : base(options.Id, hubContextHosts, logger)
        {
        }

        #region IAsyncDisposable

        public async Task DisposeAsync()
        {
            Logger.LogDebug($"Dispose");

            foreach (var disposable in this.backplaneProviders.Cast<IAsyncDisposable>())
            {
                await disposable.DisposeAsync();
            }
        }

        #endregion


        public IReadOnlyCollection<IBackplaneProvider> BackplaneProviders => this.backplaneProviders.ToList();

        public PresenceServiceMetrics GetMetrics()
        {
            return new PresenceServiceMetrics(
                Contacts.Count,
                Contacts.Count(kvp => !kvp.Value.IsSelfEmpty),
                Contacts.Sum(kvp => kvp.Value.SelfConnectionsCount),
                this.stubContacts.Count);
        }

        public void AddBackplaneProvider(IBackplaneProvider backplaneProvider)
        {
            Requires.NotNull(backplaneProvider, nameof(backplaneProvider));

            Logger.LogInformation($"AddBackplaneProvider type:{backplaneProvider.GetType().FullName}");
            this.backplaneProviders.Add(backplaneProvider);
            backplaneProvider.ContactChangedAsync = OnContactChangedAsync;
            backplaneProvider.MessageReceivedAsync = OnMessageReceivedAsync;
        }

        public async Task UpdateBackplaneMetrics(object serviceInfo, CancellationToken cancellationToken)
        {
            foreach (var backplaneProvider in this.backplaneProviders)
            {
                try
                {
                    await backplaneProvider.UpdateMetricsAsync(ServiceId, serviceInfo, GetMetrics(), cancellationToken);
                }
                catch (Exception error)
                {
                    Logger.LogError(error, $"Failed to update metrics using backplane provider:{backplaneProvider.GetType().Name}");
                }
            }
        }

        internal ConcurrentDictionary<string, Contact> Contacts { get; } = new ConcurrentDictionary<string, Contact>();

        public async Task<Dictionary<string, ConnectionProperties>> GetSelfConnectionsAsync(string contactId, CancellationToken cancellationToken)
        {
            var targetSelfContact = await GetOrCreateContactAsync(contactId, cancellationToken);
            return targetSelfContact.GetSelfConnections();
        }

        public async Task RegisterSelfContactAsync(ContactReference contactRef, Dictionary<string, object> initialProperties, CancellationToken cancellationToken)
        {
            // Note: avoid telemetry for contact id's
            using (Logger.BeginContactReferenceScope(PresenceServiceScopes.MethodRegisterSelfContact, contactRef))
            {
                Logger.LogInformation($"initialProperties:{initialProperties?.ConvertToString()}");
            }

            var registeredSelfContact = await GetOrCreateContactAsync(contactRef.Id, cancellationToken);
            await registeredSelfContact.RegisterSelfAsync(contactRef.ConnectionId, initialProperties, cancellationToken);

            // resolve contacts
            if (initialProperties != null)
            {
                await ResolveStubContacts(contactRef.ConnectionId, initialProperties, () => registeredSelfContact, cancellationToken);
            }
        }

        public async Task<Dictionary<string, object>[]> RequestSubcriptionsAsync(
            ContactReference contactRef,
            Dictionary<string, object>[] targetContactProperties,
            string[] propertyNames,
            bool useStubContact,
            CancellationToken cancellationToken)
        {
            using (Logger.BeginContactReferenceScope(PresenceServiceScopes.MethodRequestSubcriptions, contactRef))
            {
                Logger.LogDebug($"targetContactIds:{string.Join(",", targetContactProperties.Select(d => d.ConvertToString()))} propertyNames:{string.Join(",", propertyNames)}");
            }

            // placeholder for all our results that by default are null
            var requestResult = new Dictionary<string, object>[targetContactProperties.Length];

            var targetContactIds = new List<string>();
            var matchingProperties = new List<(int, Dictionary<string, object>)>();
            for (var index = 0; index < targetContactProperties.Length; ++index)
            {
                var item = targetContactProperties[index];
                if (item.TryGetValue(Properties.IdReserved, out var targetContactId))
                {
                    // target contact is known so we can add a subscription right away
                    requestResult[index] = await AddSubcriptionAsync(contactRef, targetContactId.ToString(), propertyNames, cancellationToken);
                }
                else
                {
                    // store both index & the matching properties to look later
                    matchingProperties.Add((index, item));
                }
            }

            if (matchingProperties.Count > 0)
            {
                // we may need the registered contact instance
                var registeredSelfContact = GetRegisteredContact(contactRef.Id);

                var allMatchingProperties = matchingProperties.Select(i => i.Item2).ToArray();
                var matchContactsResults = await MatchContactsAsync(allMatchingProperties, cancellationToken);
                for (var index = 0; index < matchContactsResults.Length; ++index)
                {
                    var itemResult = matchContactsResults[index];
                    var resultIndex = matchingProperties[index].Item1;
                    if (itemResult?.Count > 0)
                    {
                        // only if found fill bucket placeholder
                        requestResult[resultIndex] = await AddSubcriptionAsync(contactRef, itemResult.First().Key.ToString(), propertyNames, cancellationToken);
                    }
                    else
                    {
                        // define the matching properties intended for this bucket
                        var matchProperties = allMatchingProperties[index];

                        // look on our backplane providers
                        var backplaneContacts = await GetContactsBackplaneProvidersAsync(matchProperties, cancellationToken);
                        if (backplaneContacts.Count > 0)
                        {
                            var matchContactPair = backplaneContacts.First();
                            var backplaneProperties = matchContactPair.Value.GetAggregatedProperties();

                            // return matched properties of first contact
                            requestResult[resultIndex] = backplaneProperties;

                            // ensure the contact is created
                            var targetContactId = matchContactPair.Key;
                            var targetContact = GetOrCreateContact(targetContactId);
                            SetOtherContactData(targetContact, matchContactPair.Value);

                            // inject property 'IdReserved'
                            backplaneProperties[Properties.IdReserved] = targetContactId;

                            // add target/subscription
                            registeredSelfContact.AddTargetContacts(contactRef.ConnectionId, new string[] { targetContactId });
                            targetContact.AddSubcriptionProperties(contactRef.ConnectionId, null, propertyNames);
                        }
                        else if (useStubContact)
                        {
                            // Note: if we arrive here we will need to find/create a stub contact that will serve as
                            // a placeholder to later match the contact that will register
                            var stubContact = this.stubContacts.Values.FirstOrDefault(item => item.MatchProperties.EqualsProperties(matchProperties));
                            if (stubContact == null)
                            {
                                // no match but we want to return stub contact if later we match a new contact
                                stubContact = new StubContact(this, Guid.NewGuid().ToString(), matchProperties);
                                this.stubContacts.TryAdd(stubContact.ContactId, stubContact);
                            }

                            // add target contact to current registered contact
                            registeredSelfContact.AddTargetContacts(contactRef.ConnectionId, new string[] { stubContact.ContactId });

                            // add a connection subscription
                            stubContact.AddSubcriptionProperties(contactRef.ConnectionId, null, propertyNames);

                            // return the stub by providing the temporary stub contact id
                            requestResult[resultIndex] = new Dictionary<string, object>()
                            {
                                { Properties.IdReserved, stubContact.ContactId },
                            }; ;
                        }
                    }
                }
            }

            return requestResult;
        }

        public async Task<Dictionary<string, Dictionary<string, object>>> AddSubcriptionsAsync(
            ContactReference contactReference,
            ContactReference[] targetContacts,
            string[] propertyNames,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (Logger.BeginContactReferenceScope(PresenceServiceScopes.MethodAddSubcriptions, contactReference))
            {
                Logger.LogDebug($"targetContactIds:{string.Join(",", targetContacts.Select(c => c.Id))} propertyNames:{string.Join(",", propertyNames)}");
            }

            var result = new Dictionary<string, Dictionary<string, object>>();

            var registeredSelfContact = GetRegisteredContact(contactReference.Id);
            registeredSelfContact.AddTargetContacts(contactReference.ConnectionId, targetContacts.Select(c => c.Id).ToArray());
            foreach (var targetContact in targetContacts)
            {
                var targetSelfContact = await GetOrCreateContactAsync(targetContact.Id, cancellationToken);
                var targetContactProperties = targetSelfContact.CreateSubcription(contactReference.ConnectionId, targetContact.ConnectionId, propertyNames);
                result[targetContact.Id] = targetContactProperties;
            }

            return result;
        }

        public void RemoveSubscription(ContactReference contactReference, ContactReference[] targetContacts, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (Logger.BeginContactReferenceScope(PresenceServiceScopes.MethodRemoveSubscription, contactReference))
            {
                Logger.LogDebug($"targetContacts:{string.Join(",", targetContacts)}");
            }


            var registeredSelfContact = GetRegisteredContact(contactReference.Id);
            registeredSelfContact.RemovedTargetContacts(contactReference.ConnectionId, targetContacts.Select(c => c.Id).ToArray());
            foreach (var targetContact in targetContacts)
            {
                if (this.stubContacts.TryGetValue(targetContact.Id, out var stubContact))
                {
                    stubContact.RemoveSubscription(contactReference.ConnectionId, targetContact.ConnectionId);
                    if (!stubContact.HasSubscriptions)
                    {
                        RemoveStubContact(stubContact);
                    }
                }
                else if (Contacts.TryGetValue(targetContact.Id, out var targetSelfContact))
                {
                    targetSelfContact.RemoveSubscription(contactReference.ConnectionId, targetContact.ConnectionId);
                }
            }
        }

        public async Task UpdatePropertiesAsync(ContactReference contactReference, Dictionary<string, object> properties, CancellationToken cancellationToken)
        {
            using (Logger.BeginContactReferenceScope(PresenceServiceScopes.MethodUpdateProperties, contactReference))
            {
                Logger.LogDebug($"properties:{properties.ConvertToString()}");
            }

            var registeredSelfContact = GetRegisteredContact(contactReference.Id);
            await registeredSelfContact.UpdatePropertiesAsync(contactReference.ConnectionId, properties, cancellationToken);

            // stub contacts
            foreach (var stubContact in GetStubContacts(contactReference.Id))
            {
                await stubContact.SendUpdatePropertiesAsync(
                    contactReference.ConnectionId,
                    ContactDataProvider.CreateContactDataProvider(() => registeredSelfContact.GetAggregatedProperties()),
                    properties.Keys,
                    cancellationToken);
            }
        }

        public async Task SendMessageAsync(ContactReference contactReference, ContactReference targetContactReference, string messageType, object body, CancellationToken cancellationToken)
        {
            using (Logger.BeginContactReferenceScope(PresenceServiceScopes.MethodSendMessage, contactReference))
            {
                Logger.LogDebug($"targetContact:{targetContactReference} messageType:{messageType} body:{body}");
            }

            // Note: next line will enforce the contact who attempt to send the message to be already registered
            // otherwise an exception will happen
            GetRegisteredContact(contactReference.Id);

            if (this.stubContacts.TryGetValue(targetContactReference.Id, out var stubContact) && stubContact.ResolvedContact != null)
            {
                var resolvedContactId = stubContact.ResolvedContact.ContactId;
                Logger.LogDebug($"ResolvedContact -> targetContactId:{targetContactReference.Id} resolvedContactId:{resolvedContactId}");
                targetContactReference = new ContactReference(resolvedContactId, targetContactReference.ConnectionId);
            }

            var targetContact = GetRegisteredContact(targetContactReference.Id, throwIfNotFound: false);
            if (targetContact?.IsSelfEmpty == false)
            {
                await targetContact.SendReceiveMessageAsync(contactReference, messageType, body, targetContactReference.ConnectionId, cancellationToken);
            }

            // always attempt to notify trough the backplane 
            var messageData = new MessageData(
                contactReference,
                targetContactReference,
                messageType,
                body);
            await SendBackplaneProvidersMessagesAsync(messageData, cancellationToken);
        }

        public async Task UnregisterSelfContactAsync(ContactReference contactReference, Func<IEnumerable<string>, Task> affectedPropertiesTask, CancellationToken cancellationToken)
        {
            using (Logger.BeginContactReferenceScope(PresenceServiceScopes.MethodUnregisterSelfContact, contactReference))
            {
                Logger.LogDebug($"Unregister self contact...");
            }

            var registeredSelfContact = GetRegisteredContact(contactReference.Id);
            foreach (var targetContactId in registeredSelfContact.GetTargetContacts(contactReference.ConnectionId))
            {
                if (this.stubContacts.TryGetValue(targetContactId, out var stubContact))
                {
                    stubContact.RemoveAllSubscriptions(contactReference.ConnectionId);
                    if (!stubContact.HasSubscriptions)
                    {
                        RemoveStubContact(stubContact);
                    }
                }
                else
                {
                    GetRegisteredContact(targetContactId, false)?.RemoveAllSubscriptions(contactReference.ConnectionId);
                }
            }

            await registeredSelfContact.RemoveSelfConnectionAsync(contactReference.ConnectionId, affectedPropertiesTask, cancellationToken);
        }

        public virtual Task<Dictionary<string, Dictionary<string, object>>[]> MatchContactsAsync(Dictionary<string, object>[] matchingPropertes, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (Logger.BeginSingleScope(
                 (LoggerScopeHelpers.MethodScope, PresenceServiceScopes.MethodMatchContacts)))
            {
                Logger.LogDebug($"matchingPropertes:[{string.Join(",", matchingPropertes.Select(a => a.ConvertToString()))}]");
            }

            var results = new Dictionary<string, Dictionary<string, object>>[matchingPropertes.Length];
            foreach (var contactKvp in Contacts)
            {
                var allProperties = contactKvp.Value.GetAggregatedProperties();
                for (var index = 0; index < matchingPropertes.Length; ++index)
                {
                    if (MatchContactProperties(matchingPropertes[index], allProperties))
                    {
                        if (results[index] == null)
                        {
                            results[index] = new Dictionary<string, Dictionary<string, object>>();
                        }

                        results[index].Add(contactKvp.Key, allProperties);
                    }
                }
            }

            return Task.FromResult(results);
        }

        public virtual Task<Dictionary<string, Dictionary<string, object>>> SearchContactsAsync(Dictionary<string, SearchProperty> searchProperties, int? maxCount, CancellationToken cancellationToken = default(CancellationToken))
        {
            var searchPropertiesRegExp = searchProperties.ToDictionary(kvp => kvp.Key, kvp =>
            {
                if (string.IsNullOrEmpty(kvp.Value.Expression))
                {
                    return null;
                }

                return new Regex(kvp.Value.Expression, kvp.Value.Options.HasValue ? (RegexOptions)kvp.Value.Options.Value : RegexOptions.None);
            });

            var result = new Dictionary<string, Dictionary<string, object>>();
            foreach (var contactKvp in Contacts)
            {
                if (maxCount.HasValue && result.Count > maxCount.Value)
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                var contactProperties = contactKvp.Value.GetAggregatedProperties();

                if (searchPropertiesRegExp.Any(kvp =>
                {
                    object value;
                    contactProperties.TryGetValue(kvp.Key, out value);
                    if (kvp.Value == null)
                    {
                        return value != null;
                    }
                    else if (value == null)
                    {
                        return true;
                    }

                    return !kvp.Value.IsMatch(value.ToString());
                }))
                {
                    continue;
                }

                result.Add(contactKvp.Key, contactProperties);
            }

            return Task.FromResult(result);
        }

        protected virtual bool MatchContactProperties(
            Dictionary<string, object> matchingPropertes,
            Dictionary<string, object> contactProperties)
        {
            return matchingPropertes.MatchProperties(contactProperties);
        }

        internal Contact CreateContact(string contactId)
        {
            var contact = new Contact(this, contactId);
            contact.Changed += OnContactChangedAsync;
            return contact;
        }

        private Contact GetOrCreateContact(string contactId)
        {
            return GetOrCreateContact(contactId, out var created);
        }

        private Contact GetOrCreateContact(string contactId, out bool created)
        {
            bool added = false;
            var contact = Contacts.GetOrAdd(contactId, (key) =>
            {
                added = true;
                return CreateContact(contactId);
            });
            created = added;
            return contact;
        }

        private async Task<Contact> GetOrCreateContactAsync(string contactId, CancellationToken cancellationToken)
        {
            bool created;
            var contact = GetOrCreateContact(contactId, out created);
            if (created)
            {
                var contactDataInfo = await GetContactBackplaneContactDataAsync(contactId, cancellationToken);
                if (contactDataInfo != null)
                {
                    SetOtherContactData(contact, contactDataInfo);
                }
            }

            return contact;
        }

        private Contact GetRegisteredContact(string contactId, bool throwIfNotFound = true)
        {
            if (Contacts.TryGetValue(contactId, out var selfContact))
            {
                return selfContact;
            }

            if (throwIfNotFound)
            {
                throw new HubException($"No registration self contact found for:{contactId}");
            }

            return null;
        }

        private StubContact[] GetStubContacts(string contactId)
        {
            if (this.resolvedContacts.TryGetValue(contactId, out var stubContacts))
            {
                return stubContacts.Values.ToArray();
            }

            return Array.Empty<StubContact>();
        }

        private void RemoveStubContact(StubContact stubContact)
        {
            Logger.LogDebug($"StubContact removed -> contactId:{stubContact.ContactId}");

            this.stubContacts.TryRemove(stubContact.ContactId, out var __);
            foreach (var stubContacts in this.resolvedContacts.Values)
            {
                stubContacts.TryRemove(stubContact);
            }
        }

        private async Task ResolveStubContacts(
            string connectionId,
            Dictionary<string, object> initialProperties,
            Func<Contact> selfContactFactory,
            CancellationToken cancellationToken)
        {
            foreach (var stubContact in this.stubContacts.Values.Where(i => i.ResolvedContact == null))
            {
                if (MatchContactProperties(stubContact.MatchProperties, initialProperties))
                {
                    var selfContact = selfContactFactory();
                    stubContact.ResolvedContact = selfContact;
                    this.resolvedContacts.AddOrUpdate(selfContact.ContactId, (key) =>
                    {
                        var set = new ConcurrentHashSet<StubContact>(new StubContactComparer());
                        set.Add(stubContact);
                        return set;
                    },
                    (key, value) =>
                    {
                        value.Add(stubContact);
                        return value;
                    });

                    await stubContact.SendUpdatePropertiesAsync(connectionId,
                        ContactDataProvider.CreateContactDataProvider(initialProperties),
                        initialProperties.Keys,
                        cancellationToken);
                }
            }
        }

        private async Task OnContactChangedAsync(object sender, ContactChangedEventArgs e)
        {
            var contact = (Contact)sender;

            foreach (var backplaneProvider in this.backplaneProviders)
            {
                try
                {
                    await backplaneProvider.UpdateContactAsync(
                        new ContactDataChanged<ConnectionProperties>(
                            ServiceId,
                            e.ConectionId,
                            contact.ContactId,
                            e.ChangeType,
                            e.Properties),
                            CancellationToken.None);
                }
                catch (Exception error)
                {
                    if (ShouldLogException(error))
                    {
                        Logger.LogError(error, $"Failed to update contact using backplane provider:{backplaneProvider.GetType().Name} contactId:{contact.ContactId}");
                    }
                }
            }
        }

        private async Task SendBackplaneProvidersMessagesAsync(
            MessageData messageData,
            CancellationToken cancellationToken)
        {
            foreach (var backplaneProvider in this.backplaneProviders.OrderByDescending(p => p.Priority))
            {
                try
                {
                    await backplaneProvider.SendMessageAsync(ServiceId, messageData, cancellationToken);
                    break;
                }
                catch (Exception error)
                {
                    if (ShouldLogException(error))
                    {
                        Logger.LogError(error, $"Failed to send message using backplane provider:{backplaneProvider.GetType().Name}");
                    }
                }
            }
        }

        private async Task<ContactDataInfo> GetContactBackplaneContactDataAsync(string contactId, CancellationToken cancellationToken)
        {
            foreach (var backplaneProvider in this.backplaneProviders.OrderByDescending(p => p.Priority))
            {
                try
                {
                    var contactData = await backplaneProvider.GetContactDataAsync(contactId, cancellationToken);
                    if (contactData != null)
                    {
                        return contactData;
                    }
                }
                catch (Exception error)
                {
                    if (ShouldLogException(error))
                    {
                        Logger.LogError(error, $"Failed to get contact data entity using backplane provider:{backplaneProvider.GetType().Name} contactId:{contactId}");
                    }
                }
            }

            return null;
        }

        private async Task<Dictionary<string, ContactDataInfo>> GetContactsBackplaneProvidersAsync(Dictionary<string, object> matchProperties, CancellationToken cancellationToken)
        {
            foreach (var backplaneProvider in this.backplaneProviders.OrderByDescending(p => p.Priority))
            {
                try
                {
                    var contacts = await backplaneProvider.GetContactsDataAsync(matchProperties, cancellationToken);
                    if (contacts?.Count > 0)
                    {
                        return contacts;
                    }
                }
                catch (Exception error)
                {
                    if (ShouldLogException(error))
                    {
                        Logger.LogError($"Failed to get contacts using backplane provider:{backplaneProvider.GetType().Name}. Error:{error}");
                    }
                }
            }

            return new Dictionary<string, ContactDataInfo>();
        }

        private async Task<Dictionary<string, object>> AddSubcriptionAsync(ContactReference contactReference, string targetContactId, string[] propertyNames, CancellationToken cancellationToken)
        {
            var subscriptionResult = (await AddSubcriptionsAsync(contactReference, new ContactReference[] { new ContactReference(targetContactId, null) }, propertyNames, cancellationToken)).First().Value;
            subscriptionResult[Properties.IdReserved] = targetContactId;
            return subscriptionResult;
        }

        private async Task OnContactChangedAsync(
            ContactDataChanged<ContactDataInfo> contactDataChanged,
            string[] affectedProperties,
            CancellationToken cancellationToken)
        {
            // ignore self notifications
            if (contactDataChanged.ServiceId == ServiceId)
            {
                return;
            }

            var contactDataProvider = ContactDataProvider.CreateContactDataProvider(contactDataChanged.Data);

            if (contactDataChanged.Type == ContactUpdateType.Registration)
            {
                var lazySelfContactFactory = new Lazy<Contact>(() =>
                {
                    var selfContact = GetOrCreateContact(contactDataChanged.ContactId);
                    if (contactDataChanged.Data != null)
                    {
                        SetOtherContactData(selfContact, contactDataChanged.Data);
                    }

                    return selfContact;
                });

                await ResolveStubContacts(
                    contactDataChanged.ConnectionId,
                    contactDataProvider.Properties,
                    () => lazySelfContactFactory.Value, cancellationToken);
            }
            else
            {
                foreach (var stubContact in GetStubContacts(contactDataChanged.ContactId))
                {
                    await stubContact.SendUpdatePropertiesAsync(
                        contactDataChanged.ConnectionId,
                        contactDataProvider,
                        affectedProperties,
                        cancellationToken);
                }
            }

            if (Contacts.TryGetValue(contactDataChanged.ContactId, out var contact))
            {
                await contact.OnContactChangedAsync(contactDataChanged.Clone(GetServiceConnectionProperties(contactDataChanged.Data)), affectedProperties, cancellationToken);
            }
        }

        private async Task OnMessageReceivedAsync(
            string sourceId,
            MessageData messageData,
            CancellationToken cancellationToken)
        {
            // ignore self notifications
            if (sourceId == ServiceId)
            {
                return;
            }

            if (Contacts.TryGetValue(messageData.TargetContact.Id, out var targetContact))
            {
                await targetContact.SendReceiveMessageAsync(
                    messageData.FromContact,
                    messageData.Type,
                    messageData.Body,
                    messageData.TargetContact.ConnectionId,
                    cancellationToken);
            }
        }

        /**
         * Return the service connection properties by removing our self service instance
         * Note: the 'ContactDataInfo' structure has all the connections per service and so the need
         * to remove the 'self' connections to avoid duplications
         */
        private Dictionary<string, ConnectionProperties> GetServiceConnectionProperties(ContactDataInfo contactDataInfo)
        {
            return contactDataInfo.Where(kvp => kvp.Key != ServiceId)
                .Select(kvp => kvp.Value)
                .SelectMany(i => i)
                .ToDictionary(p => p.Key, p => p.Value);
        }

        private void SetOtherContactData(Contact contact, ContactDataInfo contactDataInfo)
        {
            Debug.Assert(contact != null, "contact == null");
            Debug.Assert(contactDataInfo != null, "contactDataInfo == null");

            contact.SetOtherConnectionProperties(GetServiceConnectionProperties(contactDataInfo));
        }

        private class StubContactComparer : IEqualityComparer<StubContact>
        {
            public bool Equals(StubContact x, StubContact y)
            {
                return x.ContactId.Equals(y.ContactId);
            }

            public int GetHashCode(StubContact obj)
            {
                return obj.ContactId.GetHashCode();
            }
        }
    
        /// <summary>
        /// Return true when this type of exception should be logged as an error to report in our telemetry
        /// </summary>
        /// <param name="error">The error instance</param>
        /// <returns></returns>
        private static bool ShouldLogException(Exception error)
        {
            return ! (error is OperationCanceledException);
        }
    }
}
