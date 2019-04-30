using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Return Contacts statistics
    /// </summary>
    public class ContactsStatistics
    {
        internal ContactsStatistics(
            int count,
            int selfCount,
            int totalSelfCount,
            int stubCount)
        {
            Count = count;
            SelfCount = selfCount;
            TotalSelfCount = totalSelfCount;
            StubCount = stubCount;
        }

        public int Count { get; }
        public int SelfCount { get; }
        public int TotalSelfCount { get; }

        public int StubCount { get; }
    }

    /// <summary>
    /// The non Hub Service class instance that manage all the registered contacts
    /// </summary>
    public class PresenceService
    {
        private readonly List<IBackplaneProvider> backplaneProviders = new List<IBackplaneProvider>();

        private readonly ConcurrentDictionary<string, StubContact> stubContacts = new ConcurrentDictionary<string, StubContact>();

        /// <summary>
        /// Map of self contact id <-> stub contact
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentHashSet<StubContact>> resolvedContacts = new ConcurrentDictionary<string, ConcurrentHashSet<StubContact>>();

        public PresenceService(IHubContext<PresenceServiceHub> hub, ILogger<PresenceService> logger)
        {
            Hub = Requires.NotNull(hub, nameof(hub));
            Logger = Requires.NotNull(logger, nameof(logger));
            ServiceId = Guid.NewGuid().ToString();

            logger.LogInformation($"Service created with id:{ServiceId}");
        }

        public string ServiceId { get; }

        public IReadOnlyCollection<IBackplaneProvider> BackplaneProviders => this.backplaneProviders.ToList();

        public ContactsStatistics GetContactStatistics()
        {
            return new ContactsStatistics(
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

        public ILogger<PresenceService> Logger { get; }

        public IHubContext<PresenceServiceHub> Hub { get; }

        internal ConcurrentDictionary<string, Contact> Contacts { get; } = new ConcurrentDictionary<string, Contact>();


        public async Task RegisterSelfContactAsync(string connectionId, string contactId, Dictionary<string, object> initialProperties, CancellationToken cancellationToken)
        {
            Logger.LogInformation($"RegisterSelfContactAsync -> connectionId:{connectionId} contactId:{contactId} initialProperties:{initialProperties.ConvertToString()}");

            var registeredSelfContact = GetOrCreateContact(contactId);
            await registeredSelfContact.RegisterSelfAsync(connectionId, initialProperties, cancellationToken);

            // resolve contacts
            await ResolveStubContacts(initialProperties, () => registeredSelfContact, cancellationToken);
        }

        public async Task<Dictionary<string, object>[]> RequestSubcriptionsAsync(
            string connectionId,
            string contactId,
            Dictionary<string, object>[] targetContactProperties,
            string[] propertyNames,
            bool useStubContact,
            CancellationToken cancellationToken)
        {
            Logger.LogDebug($"RequestSubcriptionsAsync -> connectionId:{connectionId} contactId:{contactId} targetContactIds:{string.Join(",", targetContactProperties.Select(d => d.ConvertToString()))} propertyNames:{string.Join(",", propertyNames)}");

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
                    requestResult[index] = await AddSubcriptionAsync(connectionId, contactId, targetContactId.ToString(), propertyNames, cancellationToken);
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
                var registeredSelfContact = GetRegisteredContact(contactId);

                var allMatchingProperties = matchingProperties.Select(i => i.Item2).ToArray();
                var matchContactsResults = await MatchContactsAsync(allMatchingProperties, cancellationToken);
                for (var index = 0; index < matchContactsResults.Length; ++index)
                {
                    var itemResult = matchContactsResults[index];
                    var resultIndex = matchingProperties[index].Item1;
                    if (itemResult?.Count > 0)
                    {
                        // only if found fill bucket placeholder
                        requestResult[resultIndex] = await AddSubcriptionAsync(connectionId, contactId, itemResult.First().Key.ToString(), propertyNames, cancellationToken);
                    }
                    else
                    {
                        // define the matching properties intented for this bucket
                        var matchProperties = allMatchingProperties[index];

                        // look on our backplane providers
                        var backplaneContacts = await GetContactsBackplaneProvidersAsync(matchProperties, cancellationToken);
                        if (backplaneContacts.Length > 0)
                        {
                            // return matched properties of first contact
                            requestResult[resultIndex] = backplaneContacts[0];

                            // ensure the contact is created
                            var targetContactId = backplaneContacts[0][Properties.IdReserved].ToString();
                            var targetContact = GetOrCreateContact(targetContactId);

                            // add target/subscription
                            registeredSelfContact.AddTargetContacts(connectionId, new string[] { targetContactId });
                            targetContact.AddSubcription(connectionId, propertyNames);
                        }
                        else if(useStubContact)
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
                            registeredSelfContact.AddTargetContacts(connectionId, new string[] { stubContact.ContactId });

                            // add a connection subscription
                            stubContact.AddSubcription(connectionId, propertyNames);

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

        public async Task<Dictionary<string, Dictionary<string, object>>> AddSubcriptionsAsync(string connectionId, string contactId, string[] targetContactIds, string[] propertyNames, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogDebug($"AddSubcriptionsAsync -> connectionId:{connectionId} contactId:{contactId} targetContactIds:{string.Join(",", targetContactIds)} propertyNames:{string.Join(",", propertyNames)}");

            var result = new Dictionary<string, Dictionary<string, object>>();

            var registeredSelfContact = GetRegisteredContact(contactId);
            registeredSelfContact.AddTargetContacts(connectionId, targetContactIds);
            foreach (var targetContactId in targetContactIds)
            {
                var targetSelfContact = GetOrCreateContact(targetContactId);
                var targetContactProperties = targetSelfContact.CreateSubcription(connectionId, propertyNames);
                result[targetContactId] = targetContactProperties;

                if (targetSelfContact.IsSelfEmpty)
                {
                    var properties = await GetContactBackplaneProvidersPropertiesAsync(targetContactId, cancellationToken);
                    if (properties != null)
                    {
                        foreach (var kvp in properties)
                        {
                            if (targetContactProperties.ContainsKey(kvp.Key))
                            {
                                targetContactProperties[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public void RemoveSubcriptionProperties(string connectionId, string contactId, string[] targetContactIds, string[] propertyNames, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogDebug($"RemoveSubcriptionProperties -> connectionId:{connectionId} contactId:{contactId} targetContactIds:{string.Join(",", targetContactIds)} propertyNames:{string.Join(",", propertyNames)}");
            foreach (var targetContactId in targetContactIds)
            {
                if (this.stubContacts.TryGetValue(targetContactId, out var stubContact))
                {
                    stubContact.RemoveSubcriptionProperties(connectionId, propertyNames);
                }
                else
                {
                    var targetSelfContact = GetOrCreateContact(targetContactId);
                    targetSelfContact.RemoveSubcriptionProperties(connectionId, propertyNames);
                }
            }
        }

        public void RemoveSubscription(string connectionId, string contactId, string[] targetContactIds, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogDebug($"RemoveSubscription -> connectionId:{connectionId} contactId:{contactId} targetContactIds:{string.Join(",", targetContactIds)}");

            var registeredSelfContact = GetRegisteredContact(contactId);
            registeredSelfContact.RemovedTargetContacts(connectionId, targetContactIds);
            foreach (var targetContactId in targetContactIds)
            {
                if (this.stubContacts.TryGetValue(targetContactId, out var stubContact))
                {
                    stubContact.RemoveSubscription(connectionId);
                    if (!stubContact.HasSubscriptions)
                    {
                        RemoveStubContact(stubContact);
                    }
                }
                else
                {
                    var targetSelfContact = GetOrCreateContact(targetContactId);
                    targetSelfContact.RemoveSubscription(connectionId);
                }
            }
        }

        public async Task UpdatePropertiesAsync(string connectionId, string contactId, Dictionary<string, object> updateProperties, CancellationToken cancellationToken)
        {
            Logger.LogDebug($"UpdatePropertiesAsync -> connectionId:{connectionId} contactId:{contactId} updateProperties:{updateProperties.ConvertToString()}");
            var registeredSelfContact = GetRegisteredContact(contactId);
            await registeredSelfContact.UpdatePropertiesAsync(connectionId, updateProperties, cancellationToken);

            // stub contacts
            var aggregatedProperties = new Lazy<Dictionary<string, object>>(() => registeredSelfContact.GetAggregatedProperties());
            foreach (var stubContact in GetStubContacts(contactId))
            {
                await stubContact.SendUpdatePropertiesAsync(aggregatedProperties.Value, cancellationToken);
            }
        }

        public async Task SendMessageAsync(string contactId, string targetContactId, string messageType, JToken body, CancellationToken cancellationToken)
        {
            Logger.LogDebug($"SendMessageAsync -> contactId:{contactId} targetContactId:{targetContactId} messageType:{messageType} body:{body}");

            // Note: next line will enforce the contact who attempt to send the message to be already registered
            // otherwise an exception will happen
            GetRegisteredContact(contactId);

            if (this.stubContacts.TryGetValue(targetContactId, out var stubContact) && stubContact.ResolvedContact != null)
            {
                var resolvedContactId = stubContact.ResolvedContact.ContactId;
                Logger.LogDebug($"ResolvedContact -> targetContactId:{targetContactId} resolvedContactId:{resolvedContactId}");
                targetContactId = resolvedContactId;
            }

            var targetContact = GetRegisteredContact(targetContactId, throwIfNotFound: false);
            if (targetContact?.IsSelfEmpty == false)
            {
                await targetContact.SendReceiveMessageAsync(contactId, messageType, body, cancellationToken);
            }
            else
            {
                await SendBackplaneProvidersMessagesAsync(contactId, targetContactId, messageType, body, cancellationToken);
            }
        }

        public async Task UnregisterSelfContactAsync(string connectionId, string contactId, Func<IEnumerable<string>, Task> affectedPropertiesTask, CancellationToken cancellationToken)
        {
            Logger.LogDebug($"UnregisterSelfContactAsync -> connectionId:{connectionId} contactId:{contactId}");

            var registeredSelfContact = GetRegisteredContact(contactId);
            foreach (var targetContactId in registeredSelfContact.GetTargetContacts(connectionId))
            {
                if (this.stubContacts.TryGetValue(targetContactId, out var stubContact))
                {
                    stubContact.RemoveSubscription(connectionId);
                    if (!stubContact.HasSubscriptions)
                    {
                        RemoveStubContact(stubContact);
                    }
                }
                else
                {
                    GetRegisteredContact(targetContactId, false)?.RemoveSubscription(connectionId);
                }
            }

            await registeredSelfContact.RemoveSelfConnectionAsync(connectionId, affectedPropertiesTask, cancellationToken);
        }

        public virtual Task<Dictionary<string, Dictionary<string, object>>[]> MatchContactsAsync(Dictionary<string, object>[] matchingPropertes, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogDebug($"MatchContactsAsync -> matchingPropertes:[{string.Join(",", matchingPropertes.Select(a => a.ConvertToString()))}]");

            var results = new Dictionary<string, Dictionary<string, object>>[matchingPropertes.Length];
            foreach (var contactKvp in Contacts)
            {
                var allProperties = contactKvp.Value.GetAggregatedProperties();
                for(var index=0; index < matchingPropertes.Length;++index)
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
            return Contacts.GetOrAdd(contactId, (key) => CreateContact(contactId));
        }

        private Contact GetRegisteredContact(string contactId, bool throwIfNotFound = true)
        {
            if (Contacts.TryGetValue(contactId, out var selfContact))
            {
                return selfContact;
            }

            if (throwIfNotFound)
            {
                throw new InvalidOperationException($"No registration self contact found for:{contactId}");
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
            foreach(var stubContacts in this.resolvedContacts.Values)
            {
                stubContacts.TryRemove(stubContact);
            }
        }

        private async Task ResolveStubContacts(
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
                    await stubContact.SendUpdatePropertiesAsync(initialProperties, cancellationToken);
                }
            }
        }


        private Task OnContactChangedAsync(object sender, ContactChangedEventArgs e)
        {
            var contact = (Contact)sender;
            return UpdateBackplaneProvidersPropertiesAsync(
                contact.ContactId,
                contact.GetAggregatedProperties(),
                e.Property == ContactChangedType.InitialProperties ? ContactUpdateType.Registration : ContactUpdateType.UpdateProperties,
                CancellationToken.None);
        }

        private async Task UpdateBackplaneProvidersPropertiesAsync(
            string contactId,
            Dictionary<string, object> properties,
            ContactUpdateType updateContactType,
            CancellationToken cancellationToken)
        {
            foreach (var backplaneProvider in this.backplaneProviders)
            {
                try
                {
                    await backplaneProvider.UpdateContactAsync(ServiceId, contactId, properties, updateContactType, cancellationToken);
                }
                catch (Exception error)
                {
                    Logger.LogError($"Failed to update contact properties using backplane provider:{backplaneProvider.GetType().Name} contactId:{contactId}. Error:{error}");
                }
            }
        }

        private async Task SendBackplaneProvidersMessagesAsync(string contactId, string targetContactId, string messageType, JToken body, CancellationToken cancellationToken)
        {
            foreach (var backplaneProvider in this.backplaneProviders.OrderByDescending(p => p.Priority))
            {
                try
                {
                    await backplaneProvider.SendMessageAsync(ServiceId, contactId, targetContactId, messageType, body, cancellationToken);
                    break;
                }
                catch (Exception error)
                {
                    Logger.LogError($"Failed to send message using backplane provider:{backplaneProvider.GetType().Name}. Error:{error}");
                }
            }
        }

        private async Task<Dictionary<string, object>> GetContactBackplaneProvidersPropertiesAsync(string contactId, CancellationToken cancellationToken)
        {
            foreach (var backplaneProvider in this.backplaneProviders.OrderByDescending(p => p.Priority))
            {
                try
                {
                    var properties = await backplaneProvider.GetContactPropertiesAsync(contactId, cancellationToken);
                    if (properties != null)
                    {
                        return properties;
                    }
                }
                catch (Exception error)
                {
                    Logger.LogError($"Failed to get contact properties using backplane provider:{backplaneProvider.GetType().Name} contactId:{contactId}. Error:{error}");
                }
            }

            return null;
        }

        private async Task<Dictionary<string, object>[]> GetContactsBackplaneProvidersAsync(Dictionary<string, object> matchProperties, CancellationToken cancellationToken)
        {
            foreach (var backplaneProvider in this.backplaneProviders.OrderByDescending(p => p.Priority))
            {
                try
                {
                    var contacts = await backplaneProvider.GetContactsAsync(matchProperties, cancellationToken);
                    if (contacts?.Length > 0)
                    {
                        return contacts;
                    }
                }
                catch (Exception error)
                {
                    Logger.LogError($"Failed to get contacts using backplane provider:{backplaneProvider.GetType().Name}. Error:{error}");
                }
            }

            return Array.Empty<Dictionary<string, object>>();
        }

        private async Task<Dictionary<string, object>> AddSubcriptionAsync(string connectionId, string contactId, string targetContactId, string[] propertyNames, CancellationToken cancellationToken)
        {
            var subscriptionResult = (await AddSubcriptionsAsync(connectionId, contactId, new string[] { targetContactId }, propertyNames, cancellationToken)).First().Value;
            subscriptionResult[Properties.IdReserved] = targetContactId;
            return subscriptionResult;
        }


        private async Task OnContactChangedAsync(
            string sourceId,
            string contactId,
            Dictionary<string, object> properties,
            ContactUpdateType updateContactType,
            CancellationToken cancellationToken)
        {
            if (updateContactType == ContactUpdateType.Registration)
            {
                await ResolveStubContacts(properties, () => GetOrCreateContact(contactId), cancellationToken);
            }
            else
            {
                foreach (var stubContact in GetStubContacts(contactId))
                {
                    await stubContact.SendUpdatePropertiesAsync(properties, cancellationToken);
                }
            }

            if (Contacts.TryGetValue(contactId, out var contact) && contact.IsSelfEmpty)
            {
                await contact.SendUpdatePropertiesAsync(properties, cancellationToken);
            }
        }

        private async Task OnMessageReceivedAsync(
            string sourceId,
            string contactId,
            string targetContactId,
            string type,
            JToken body,
            CancellationToken cancellationToken)
        {
            if (Contacts.TryGetValue(targetContactId, out var targetContact) && !targetContact.IsSelfEmpty)
            {
                await targetContact.SendReceiveMessageAsync(contactId, type, body, cancellationToken);
            }
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
