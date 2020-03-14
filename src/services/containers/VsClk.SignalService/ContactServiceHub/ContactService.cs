// <copyright file="ContactService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;
using ContactDataInfo = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>>;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// The non Hub Service class instance that manage all the registered contacts.
    /// </summary>
    public class ContactService : HubService<ContactServiceHub, HubServiceOptions>
    {
        private readonly ConcurrentDictionary<string, StubContact> stubContacts = new ConcurrentDictionary<string, StubContact>();

        /// <summary>
        /// Map of self contact id <-> stub contact
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentHashSet<StubContact>> resolvedContacts = new ConcurrentDictionary<string, ConcurrentHashSet<StubContact>>();

        public ContactService(
            HubServiceOptions options,
            IEnumerable<IHubContextHost> hubContextHosts,
            ILogger<ContactService> logger,
            IContactBackplaneManager backplaneManager = null,
            IDataFormatProvider formatProvider = null)
            : base(options, hubContextHosts, logger, formatProvider)
        {
            BackplaneManager = backplaneManager;

            if (backplaneManager != null)
            {
                backplaneManager.ContactChangedAsync += OnContactChangedAsync;
                backplaneManager.MessageReceivedAsync += OnMessageReceivedAsync;
                backplaneManager.MetricsFactory = () => ((ServiceId, options.Stamp), GetMetrics());
            }
        }

        public IContactBackplaneManager BackplaneManager { get; }

        internal ConcurrentDictionary<string, Contact> Contacts { get; } = new ConcurrentDictionary<string, Contact>();

        public ContactServiceMetrics GetMetrics()
        {
            return new ContactServiceMetrics(
                Contacts.Count,
                Contacts.Count(kvp => !kvp.Value.IsSelfEmpty),
                Contacts.Sum(kvp => kvp.Value.SelfConnectionsCount),
                this.stubContacts.Count);
        }

        public async Task<Dictionary<string, ConnectionProperties>> GetSelfConnectionsAsync(string contactId, CancellationToken cancellationToken)
        {
            var targetSelfContact = await GetOrCreateContactAsync(contactId, cancellationToken);
            return targetSelfContact.GetSelfConnections();
        }

        public async Task RegisterSelfContactAsync(ContactReference contactRef, Dictionary<string, object> initialProperties, CancellationToken cancellationToken)
        {
            var start = Stopwatch.StartNew();

            var registeredSelfContact = await GetOrCreateContactAsync(contactRef.Id, cancellationToken);
            await registeredSelfContact.RegisterSelfAsync(contactRef.ConnectionId, initialProperties, cancellationToken);

            // resolve contacts
            if (initialProperties != null)
            {
                await ResolveStubContacts(contactRef.ConnectionId, initialProperties, () => registeredSelfContact, cancellationToken);
            }

            // Note: avoid telemetry for contact id's
            using (Logger.BeginContactReferenceScope(
                ContactServiceScopes.MethodRegisterSelfContact,
                contactRef,
                FormatProvider,
                (LoggerScopeHelpers.MethodPerfScope, start.ElapsedMilliseconds)))
            {
                Logger.LogInformation($"initialProperties:{initialProperties?.ConvertToString(FormatProvider)}");
            }
        }

        public async Task<Dictionary<string, object>[]> RequestSubcriptionsAsync(
            ContactReference contactRef,
            Dictionary<string, object>[] targetContactProperties,
            string[] propertyNames,
            bool useStubContact,
            CancellationToken cancellationToken)
        {
            var start = Stopwatch.StartNew();

            // placeholder for all our results that by default are null
            var requestResult = new Dictionary<string, object>[targetContactProperties.Length];

            var targetContactIds = new List<string>();
            var matchingProperties = new List<(int, Dictionary<string, object>)>();
            for (var index = 0; index < targetContactProperties.Length; ++index)
            {
                var item = targetContactProperties[index];
                if (item.TryGetValue(ContactProperties.IdReserved, out var targetContactId))
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

                // look on currently registered contacts on this service
                var matchContactsResults = await MatchContactsAsync(allMatchingProperties, cancellationToken);

                // this list will collect all the non matching properties
                var nonMatchingProps = new List<(int, Dictionary<string, object>)>();

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
                        // collect our non matching properties
                        nonMatchingProps.Add((resultIndex, allMatchingProperties[index]));
                    }
                }

                // look on our backplane providers
                var backplaneMatchingContacts = BackplaneManager != null ?
                    await BackplaneManager.GetContactsDataAsync(nonMatchingProps.Select(i => i.Item2).ToArray(), cancellationToken) :
                    new Dictionary<string, ContactDataInfo>[nonMatchingProps.Count];

                Assumes.Equals(backplaneMatchingContacts.Length, nonMatchingProps.Count);

                for (int next = 0; next < backplaneMatchingContacts.Length; ++next)
                {
                    var resultIndex = nonMatchingProps[next].Item1;
                    var matchProperties = nonMatchingProps[next].Item2;

                    var backplaneContacts = backplaneMatchingContacts[next];
                    if (backplaneContacts?.Count > 0)
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
                        backplaneProperties[ContactProperties.IdReserved] = targetContactId;

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
                                { ContactProperties.IdReserved, stubContact.ContactId },
                            };
                    }
                }
            }

            using (Logger.BeginContactReferenceScope(ContactServiceScopes.MethodRequestSubcriptions, contactRef, FormatProvider, (LoggerScopeHelpers.MethodPerfScope, start.ElapsedMilliseconds)))
            {
                Logger.LogDebug($"targetContactIds:{string.Join(",", targetContactProperties.Select(d => d.ConvertToString(FormatProvider)))} propertyNames:{string.Join(",", propertyNames)}");
            }

            return requestResult;
        }

        public async Task<Dictionary<string, Dictionary<string, object>>> AddSubcriptionsAsync(
            ContactReference contactReference,
            ContactReference[] targetContacts,
            string[] propertyNames,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (Logger.BeginContactReferenceScope(ContactServiceScopes.MethodAddSubcriptions, contactReference, FormatProvider))
            {
                Logger.LogDebug($"targetContactIds:{string.Join(",", targetContacts.Select(c => FormatContactId(c.Id)))} propertyNames:{string.Join(",", propertyNames)}");
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
            using (Logger.BeginContactReferenceScope(ContactServiceScopes.MethodRemoveSubscription, contactReference, FormatProvider))
            {
                Logger.LogDebug($"targetContacts:{string.Join(",", targetContacts.Select(cr => cr.ToString(FormatProvider)))}");
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
            using (Logger.BeginContactReferenceScope(ContactServiceScopes.MethodUpdateProperties, contactReference, FormatProvider))
            {
                Logger.LogDebug($"properties:{properties.ConvertToString(FormatProvider)}");
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
            using (Logger.BeginContactReferenceScope(ContactServiceScopes.MethodSendMessage, contactReference, FormatProvider, (ContactServiceScopes.MessageTypeScope, messageType)))
            {
                Logger.LogDebug($"targetContact:{targetContactReference.ToString(FormatProvider)} messageType:{messageType} body:{Format("{0:K}", body?.ToString())}");
            }

            // Note: next line will enforce the contact who attempt to send the message to be already registered
            // otherwise an exception will happen
            GetRegisteredContact(contactReference.Id);

            if (this.stubContacts.TryGetValue(targetContactReference.Id, out var stubContact) && stubContact.ResolvedContact != null)
            {
                var resolvedContactId = stubContact.ResolvedContact.ContactId;
                Logger.LogDebug($"ResolvedContact -> targetContactId:{FormatContactId(targetContactReference.Id)} resolvedContactId:{resolvedContactId}");
                targetContactReference = new ContactReference(resolvedContactId, targetContactReference.ConnectionId);
            }

            var targetContact = GetRegisteredContact(targetContactReference.Id, throwIfNotFound: false);
            if (targetContact?.CanSendMessage(targetContactReference.ConnectionId) == true)
            {
                await targetContact.SendReceiveMessageAsync(contactReference, messageType, body, targetContactReference.ConnectionId, cancellationToken);
            }

            // always attempt to notify trough the backplane
            var messageData = new MessageData(
                Guid.NewGuid().ToString(),
                ServiceId,
                contactReference,
                targetContactReference,
                messageType,
                body);

            if (BackplaneManager != null)
            {
                await BackplaneManager.SendMessageAsync(messageData, cancellationToken);
            }
        }

        public async Task UnregisterSelfContactAsync(ContactReference contactReference, Func<IEnumerable<string>, Task> affectedPropertiesTask, CancellationToken cancellationToken)
        {
            var start = Stopwatch.StartNew();

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

            using (Logger.BeginContactReferenceScope(
                ContactServiceScopes.MethodUnregisterSelfContact,
                contactReference,
                FormatProvider,
                (LoggerScopeHelpers.MethodPerfScope, start.ElapsedMilliseconds)))
            {
                Logger.LogDebug($"Unregister self contact...");
            }
        }

        public virtual Task<Dictionary<string, Dictionary<string, object>>[]> MatchContactsAsync(Dictionary<string, object>[] matchingPropertes, CancellationToken cancellationToken = default(CancellationToken))
        {
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

            Logger.LogMethodScope(
                LogLevel.Debug,
                $"matchingPropertes:[{string.Join(",", matchingPropertes.Select(a => a.ConvertToString(FormatProvider)))}] match count:{results.Length}",
                ContactServiceScopes.MethodMatchContacts);

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

        internal string FormatContactId(string s)
        {
            return Format("{0:T}", s);
        }

        internal Contact CreateContact(string contactId)
        {
            var contact = new Contact(this, contactId);
            contact.Changed += OnContactChangedAsync;
            return contact;
        }

        protected virtual bool MatchContactProperties(
            Dictionary<string, object> matchingPropertes,
            Dictionary<string, object> contactProperties)
        {
            return matchingPropertes.MatchProperties(contactProperties);
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
                var contactDataInfo = BackplaneManager != null ? await BackplaneManager.GetContactDataAsync(contactId, cancellationToken) : null;
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

            this.stubContacts.TryRemove(stubContact.ContactId, out var removed__);
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
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
                        (key, value) =>
                        {
                            value.Add(stubContact);
                            return value;
                        });
#pragma warning restore SA1117 // Parameters should be on same line or separate lines

                    await stubContact.SendUpdatePropertiesAsync(
                        connectionId,
                        ContactDataProvider.CreateContactDataProvider(initialProperties),
                        initialProperties.Keys,
                        cancellationToken);
                }
            }
        }

        private async Task OnContactChangedAsync(object sender, ContactChangedEventArgs e)
        {
            if (BackplaneManager != null)
            {
                var contact = (Contact)sender;
                var contactDataChanged = new ContactDataChanged<ConnectionProperties>(
                                Guid.NewGuid().ToString(),
                                ServiceId,
                                e.ConectionId,
                                contact.ContactId,
                                e.ChangeType,
                                e.Properties);

                await BackplaneManager.UpdateContactAsync(contactDataChanged, CancellationToken.None);
            }
        }

        private async Task<Dictionary<string, object>> AddSubcriptionAsync(ContactReference contactReference, string targetContactId, string[] propertyNames, CancellationToken cancellationToken)
        {
            var subscriptionResult = (await AddSubcriptionsAsync(contactReference, new ContactReference[] { new ContactReference(targetContactId, null) }, propertyNames, cancellationToken)).First().Value;
            subscriptionResult[ContactProperties.IdReserved] = targetContactId;
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

            if (contactDataChanged.ChangeType == ContactUpdateType.Registration)
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
                    () => lazySelfContactFactory.Value,
                    cancellationToken);
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
            MessageData messageData,
            CancellationToken cancellationToken)
        {
            // ignore self notifications
            if (messageData.ServiceId == ServiceId)
            {
                return;
            }

            if (Contacts.TryGetValue(messageData.TargetContact.Id, out var targetContact) &&
                targetContact.CanSendMessage(messageData.TargetContact.ConnectionId))
            {
                using (Logger.BeginContactReferenceScope(ContactServiceScopes.MethodOnMessageReceived, messageData.TargetContact, FormatProvider))
                {
                    Logger.LogDebug($"fromContact:{messageData.FromContact.ToString(FormatProvider)} messageType:{messageData.Type}");
                }

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
    }
}
