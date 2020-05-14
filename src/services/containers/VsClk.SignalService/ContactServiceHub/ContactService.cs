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
        /// <summary>
        /// Storage for temporary stub contacts by mapping a generated id with the stub contact instance.
        /// </summary>
        private readonly ConcurrentDictionary<string, StubContact> stubContacts = new ConcurrentDictionary<string, StubContact>();

        /// <summary>
        /// Storage to map an email with a Stub contact (when the matching properties are just an email).
        /// </summary>
        private readonly ConcurrentDictionary<string, StubContact> stubContactsByEmail = new ConcurrentDictionary<string, StubContact>();

        /// <summary>
        /// Hold all our current stub contacts that are not yet resolved.
        /// </summary>
        private readonly ConcurrentHashSet<StubContact> unresolvedStubContacts = new ConcurrentHashSet<StubContact>(StubContactComparer.Instance);

        /// <summary>
        /// Dictionary to map email values with contact ids.
        /// </summary>
        private readonly ConcurrentDictionary<string, string> emailsIndexed = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Map of self contact id and stub contact.
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentHashSet<StubContact>> resolvedContacts = new ConcurrentDictionary<string, ConcurrentHashSet<StubContact>>();

        public ContactService(
            HubServiceOptions options,
            IEnumerable<IHubContextHost> hubContextHosts,
            ILogger<ContactService> logger,
            IContactBackplaneManager backplaneManager = null,
            IServiceCounters hubServiceCounters = null,
            IDataFormatProvider formatProvider = null)
            : base(options, hubContextHosts, logger, hubServiceCounters, formatProvider)
        {
            BackplaneManager = backplaneManager;
            if (backplaneManager != null)
            {
                backplaneManager.ContactChangedAsync += OnContactChangedAsync;
                backplaneManager.MessageReceivedAsync += OnMessageReceivedAsync;
                backplaneManager.MetricsFactory = () => (new ServiceInfo(ServiceId, options.Stamp, nameof(ContactService)), GetMetrics());
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
                await ResolveStubContactsAsync(contactRef.ConnectionId, initialProperties, () => registeredSelfContact, cancellationToken);
            }

            // Note: avoid telemetry for contact id's
            using (Logger.BeginContactReferenceScope(
                ContactServiceScopes.MethodRegisterSelfContact,
                contactRef,
                FormatProvider,
                (LoggerScopeHelpers.MethodPerfScope, start.ElapsedMilliseconds)))
            {
                Logger.LogDebug($"initialProperties:{initialProperties?.ConvertToString(FormatProvider)}");
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
                var backplaneMatchingContacts = BackplaneManager != null && nonMatchingProps.Count > 0 ?
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

                        // the stub contact factory callback
                        Func<StubContact> stubContactFactory = () =>
                        {
                            var stubContact = new StubContact(this, Guid.NewGuid().ToString(), matchProperties);

                            // track this stub contact
                            this.stubContacts.TryAdd(stubContact.ContactId, stubContact);
                            return stubContact;
                        };

                        StubContact stubContact;
                        if (TryEmailPropertyValue(matchProperties, onlyPropertyFlag: true, out var email))
                        {
                            // in this case the email property is just the matching value needed
                            if (!this.stubContactsByEmail.TryGetValue(email, out stubContact))
                            {
                                stubContact = stubContactFactory();

                                // additional stub contact by email.
                                this.stubContactsByEmail.TryAdd(email, stubContact);
                            }
                        }
                        else
                        {
                            stubContact = this.stubContacts.Values.FirstOrDefault(item => item.MatchProperties.EqualsProperties(matchProperties));
                            if (stubContact == null)
                            {
                                // no match but we want to return stub contact if later we match a new contact
                                stubContact = stubContactFactory();

                                // add to our list of unresolved contacts
                                // Note: resolving stub contacts could be an expensive operation and se we don't expect
                                // much of this code path when scaling up with adding a more sophisticated indexing on the
                                // registered contacts.
                                this.unresolvedStubContacts.Add(stubContact);
                            }
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
                    PurgeContactIf(targetSelfContact);
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
                Logger.LogInformation($"targetContact:{targetContactReference.ToString(FormatProvider)} messageType:{messageType}");
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
                    var targetContact = GetRegisteredContact(targetContactId, false);
                    if (targetContact != null)
                    {
                        targetContact.RemoveAllSubscriptions(contactReference.ConnectionId);

                        // purge target contact
                        PurgeContactIf(targetContact);
                    }
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

            // purge self contact
            PurgeContactIf(registeredSelfContact);
        }

        public virtual Task<Dictionary<string, Dictionary<string, object>>[]> MatchContactsAsync(Dictionary<string, object>[] matchingProperties, CancellationToken cancellationToken = default(CancellationToken))
        {
            var results = new Dictionary<string, Dictionary<string, object>>[matchingProperties.Length];
            var resolved = new bool[matchingProperties.Length];

            // Note: next block will resolve the matching properties for the optimized case
            // that just the email is being passed as a matching value and so we will use our
            // email indexing dictionary
            for (var index = 0; index < matchingProperties.Length; ++index)
            {
                if (TryEmailPropertyValue(matchingProperties[index], onlyPropertyFlag: true, out var email))
                {
                    resolved[index] = true;
                    if (this.emailsIndexed.TryGetValue(email, out var contactId)
                        && Contacts.TryGetValue(contactId, out var contact))
                    {
                        results[index] = new Dictionary<string, Dictionary<string, object>>();
                        results[index].Add(contactId, contact.GetAggregatedProperties());
                    }
                }
            }

            if (resolved.Any(resolved => !resolved))
            {
                foreach (var contactKvp in Contacts)
                {
                    var allProperties = contactKvp.Value.GetAggregatedProperties();
                    for (var index = 0; index < matchingProperties.Length; ++index)
                    {
                        if (!resolved[index])
                        {
                            if (MatchContactProperties(matchingProperties[index], allProperties))
                            {
                                if (results[index] == null)
                                {
                                    results[index] = new Dictionary<string, Dictionary<string, object>>();
                                }

                                results[index].Add(contactKvp.Key, allProperties);
                            }
                        }
                    }
                }
            }

            Logger.LogMethodScope(
                LogLevel.Debug,
                $"matchingPropertes:[{string.Join(",", matchingProperties.Select(a => a.ConvertToString(FormatProvider)))}] match count:{results.Length}",
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

        /// <summary>
        /// Check if some matching properties are only an email property value.
        /// </summary>
        /// <param name="matchingProperties">The macthing properties beign checked.</param>
        /// <param name="onlyPropertyFlag">if we expect just one property to be present.</param>
        /// <param name="email">The email value if found.</param>
        /// <returns>true if just one property is being found and it has a valid value.</returns>
        private static bool TryEmailPropertyValue(Dictionary<string, object> matchingProperties, bool onlyPropertyFlag, out string email)
        {
            if ((!onlyPropertyFlag || matchingProperties.Count == 1) && matchingProperties.TryGetValue(ContactProperties.Email, out var emailValue) && emailValue != null)
            {
                email = emailValue.ToString();
                return !string.IsNullOrEmpty(email);
            }

            email = null;
            return false;
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

            if (TryEmailPropertyValue(stubContact.MatchProperties, onlyPropertyFlag: true, out var email))
            {
                this.stubContactsByEmail.TryRemove(email, out var removed2__);
            }

            foreach (var resolvedContactKvp in this.resolvedContacts.ToArray())
            {
                resolvedContactKvp.Value.TryRemove(stubContact);
                if (resolvedContactKvp.Value.Count == 0)
                {
                    this.resolvedContacts.TryRemove(resolvedContactKvp.Key, out var removed2__);
                }
            }
        }

        private async Task ResolveStubContactsAsync(
            string connectionId,
            Dictionary<string, object> initialProperties,
            Func<Contact> selfContactFactory,
            CancellationToken cancellationToken)
        {
            // this is the expected and more efficient code that use the emails indexed stub contacts
            if (TryEmailPropertyValue(initialProperties, onlyPropertyFlag: false, out var email)
                && this.stubContactsByEmail.TryGetValue(email, out var stubContact)
                && stubContact.ResolvedContact == null)
            {
                await ResolveStubContactAsync(stubContact, connectionId, initialProperties, selfContactFactory, cancellationToken);
            }

            // next block was maintained for backward compatibility and will attempt to look in all the unresolved stub contacts.
            foreach (var unresolvedStubContact in this.unresolvedStubContacts.Values.Where(i => i.ResolvedContact == null))
            {
                if (MatchContactProperties(unresolvedStubContact.MatchProperties, initialProperties))
                {
                    await ResolveStubContactAsync(unresolvedStubContact, connectionId, initialProperties, selfContactFactory, cancellationToken);
                    this.unresolvedStubContacts.TryRemove(unresolvedStubContact);
                }
            }
        }

        private async Task ResolveStubContactAsync(
            StubContact stubContact,
            string connectionId,
            Dictionary<string, object> initialProperties,
            Func<Contact> selfContactFactory,
            CancellationToken cancellationToken)
        {
            var selfContact = selfContactFactory();
            stubContact.ResolvedContact = selfContact;
            this.resolvedContacts.AddOrUpdate(
                selfContact.ContactId,
                (key) =>
                {
                    var set = new ConcurrentHashSet<StubContact>(StubContactComparer.Instance);
                    set.Add(stubContact);
                    return set;
                },
                (key, value) =>
                {
                    value.Add(stubContact);
                    return value;
                });

            await stubContact.SendUpdatePropertiesAsync(
                connectionId,
                ContactDataProvider.CreateContactDataProvider(initialProperties),
                initialProperties.Keys,
                cancellationToken);
        }

        /// <summary>
        /// Method to purge a contact when the contact does not have any self connection endpoint & has no subscription.
        /// </summary>
        /// <param name="contact">The contact entity.</param>
        private void PurgeContactIf(Contact contact)
        {
            if (contact.IsSelfEmpty && !contact.HasSubscriptions)
            {
                Logger.LogMethodScope(
                    LogLevel.Debug,
                    $"Purge contact id:{FormatContactId(contact.ContactId)}",
                    ContactServiceScopes.MethodPurgeContact);
                MethodPerf(ContactServiceScopes.MethodPurgeContact, TimeSpan.Zero);
                Contacts.TryRemove(contact.ContactId, out var removed__);
            }
        }

        private async Task OnContactChangedAsync(object sender, ContactChangedEventArgs e)
        {
            var contact = (Contact)sender;

            // next block will index the email property to later resolve request subscription by email
            if (e.Properties.TryGetValue(ContactProperties.Email, out var pv)
                && !string.IsNullOrEmpty(pv.Value?.ToString()))
            {
                var email = pv.Value.ToString();
                this.emailsIndexed.TryAdd(email, contact.ContactId);
            }

            if (BackplaneManager != null)
            {
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

            var sw = Stopwatch.StartNew();
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

                await ResolveStubContactsAsync(
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

            MethodPerf(nameof(OnContactChangedAsync), sw.Elapsed);
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

            var sw = Stopwatch.StartNew();
            if (Contacts.TryGetValue(messageData.TargetContact.Id, out var targetContact) &&
                targetContact.CanSendMessage(messageData.TargetContact.ConnectionId))
            {
                using (Logger.BeginContactReferenceScope(ContactServiceScopes.MethodOnMessageReceived, messageData.TargetContact, FormatProvider))
                {
                    Logger.LogInformation($"changeId:{messageData.ChangeId} fromContact:{messageData.FromContact.ToString(FormatProvider)} messageType:{messageData.Type}");
                }

                // Note: since we typically receive the message trough a backplane rpc callback
                // we will extract a 'raw' object instead of passing the JObject/JToken type in case
                // the serialization was done using the Newtonsoft lib
                await targetContact.SendReceiveMessageAsync(
                    messageData.FromContact,
                    messageData.Type,
                    NewtonsoftHelpers.ToRawObject(messageData.Body),
                    messageData.TargetContact.ConnectionId,
                    cancellationToken);
            }

            MethodPerf(nameof(OnMessageReceivedAsync), sw.Elapsed);
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
            public static readonly StubContactComparer Instance = new StubContactComparer();

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
