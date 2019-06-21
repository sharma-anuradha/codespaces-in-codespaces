using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// The SignalR Hub class for the presence service
    /// </summary>
    public class PresenceServiceHub : Hub<IPresenceServiceClientHub>, IPresenceServiceHub
    {
        private const int ContactIdKey = 1;
        /// <summary>
        /// Note: this is the delay we used to avoid glitches when a disconnection happen but
        /// later another connection is established and so we should not notify prematurly
        /// </summary>
        private const int DisconnectSubscriptionDelaySecs = 10;

        private readonly PresenceService presenceService;
        private readonly ILogger logger;

        public PresenceServiceHub(PresenceService presenceService, ILogger<PresenceServiceHub> logger)
        {
            this.presenceService = presenceService ?? throw new ArgumentNullException(nameof(presenceService));
            this.logger = logger;
        }

        public Task<Dictionary<string, IDictionary<string, PropertyValue>>> GetSelfConnectionsAsync(string contactId)
        {
            return this.presenceService.GetSelfConnectionsAsync(contactId, Context.ConnectionAborted);
        }

        public async Task<Dictionary<string, object>> RegisterSelfContactAsync(string contactId, Dictionary<string, object> initialProperties)
        {
            contactId = GetContactIdentity(contactId);
            Requires.NotNullOrEmpty(contactId, nameof(contactId));
            Context.Items[ContactIdKey] = contactId;

            var contactReference = new ContactReference(contactId, Context.ConnectionId);
            await this.presenceService.RegisterSelfContactAsync(
                contactReference,
                initialProperties, 
                Context.ConnectionAborted);
            return new Dictionary<string, object>
            {
                { Properties.ContactId, contactReference.Id },
                { Properties.ConnectionId, contactReference.ConnectionId },
                { Properties.ServiceId, presenceService.ServiceId },
            };
        }

        public Task PublishPropertiesAsync(Dictionary<string, object> updateProperties)
        {
            return this.presenceService.UpdatePropertiesAsync(GetContextContactReference(), updateProperties, Context.ConnectionAborted);
        }
     
        public Task<Dictionary<string, object>[]> RequestSubcriptionsAsync(Dictionary<string, object>[] targetContactProperties, string[] propertyNames, bool useStubContact)
        {
            return this.presenceService.RequestSubcriptionsAsync(GetContextContactReference(), targetContactProperties, propertyNames, useStubContact, Context.ConnectionAborted);
        }

        public Task<Dictionary<string, Dictionary<string, object>>> AddSubcriptionsAsync(ContactReference[] targetContacts, string[] propertyNames)
        {
            return this.presenceService.AddSubcriptionsAsync(GetContextContactReference(), targetContacts, propertyNames, Context.ConnectionAborted);
        }

        public void RemoveSubscription(ContactReference[] targetContacts)
        {
            var contextRegisteredContactRef = GetContextContactReference(throwIfNotFound: false);
            if (!string.IsNullOrEmpty(contextRegisteredContactRef.Id))
            {
                this.presenceService.RemoveSubscription(contextRegisteredContactRef, targetContacts);
            }
        }

        public Task SendMessageAsync(ContactReference targetContact, string messageType, object body)
        {
            return this.presenceService.SendMessageAsync(
                GetContextContactReference(),
                targetContact,
                messageType,
                body,
                Context.ConnectionAborted);
        }

        public async Task UnregisterSelfContactAsync()
        {
            var contextRegisteredContactRef = GetContextContactReference(throwIfNotFound: false);
            if (!string.IsNullOrEmpty(contextRegisteredContactRef.Id))
            {
                await this.presenceService.UnregisterSelfContactAsync(
                    contextRegisteredContactRef,
                    null,
                    Context.ConnectionAborted);
            }
        }

        public Task<Dictionary<string, Dictionary<string, object>>[]> MatchContactsAsync(Dictionary<string, object>[] matchingProperties)
        {
            return this.presenceService.MatchContactsAsync(matchingProperties, Context.ConnectionAborted);
        }

        public Task<Dictionary<string, Dictionary<string, object>>> SearchContactsAsync(Dictionary<string, SearchProperty> searchProperties, int? maxCount)
        {
            return this.presenceService.SearchContactsAsync(searchProperties, maxCount, Context.ConnectionAborted);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var contextRegisteredContactRef = GetContextContactReference(throwIfNotFound: false);
            if (!string.IsNullOrEmpty(contextRegisteredContactRef.Id))
            {
                this.logger.LogDebug($"OnDisconnectedAsync contextRegisteredContactRef:{contextRegisteredContactRef}");
                await this.presenceService.UnregisterSelfContactAsync(
                    contextRegisteredContactRef,
                    (properties) => Task.Delay(TimeSpan.FromSeconds(DisconnectSubscriptionDelaySecs)),
                    default);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Return the proper contact identity
        /// </summary>
        /// <param name="contactId">The client request contact id</param>
        /// <returns></returns>
        protected virtual string GetContactIdentity(string contactId)
        {
            return contactId;
        }

        private string GetContextRegisteredContactId(bool throwIfNotFound = true)
        {
            object registeredContactId;
            if (!Context.Items.TryGetValue(ContactIdKey, out registeredContactId) && throwIfNotFound)
            {
                throw new InvalidOperationException($"No context contact id found for connection id:{Context.ConnectionId}");
            }

            return registeredContactId?.ToString();
        }

        private ContactReference GetContextContactReference(bool throwIfNotFound = true)
        {
            var registeredContactId = GetContextRegisteredContactId(throwIfNotFound);
            return string.IsNullOrEmpty(registeredContactId) ? default : new ContactReference(registeredContactId, Context.ConnectionId);
        }
    }
}
