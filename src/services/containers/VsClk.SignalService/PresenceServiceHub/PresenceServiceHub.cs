using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

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

        public Task RegisterSelfContactAsync(string contactId, Dictionary<string, object> initialProperties)
        {
            contactId = GetContactIdentity(contactId);
            Requires.NotNullOrEmpty(contactId, nameof(contactId));
            Context.Items[ContactIdKey] = contactId;

            return this.presenceService.RegisterSelfContactAsync(Context.ConnectionId, contactId, initialProperties, Context.ConnectionAborted);
        }

        public Task PublishPropertiesAsync(Dictionary<string, object> updateProperties)
        {
            return this.presenceService.UpdatePropertiesAsync(Context.ConnectionId, GetContextRegisteredContactId(), updateProperties, Context.ConnectionAborted);
        }
     
        public Task<Dictionary<string, object>[]> RequestSubcriptionsAsync(Dictionary<string, object>[] targetContactProperties, string[] propertyNames, bool useStubContact)
        {
            return this.presenceService.RequestSubcriptionsAsync(Context.ConnectionId, GetContextRegisteredContactId(), targetContactProperties, propertyNames, useStubContact, Context.ConnectionAborted);
        }

        public Task<Dictionary<string, Dictionary<string, object>>> AddSubcriptionsAsync(string[] targetContactIds, string[] propertyNames)
        {
            return this.presenceService.AddSubcriptionsAsync(Context.ConnectionId, GetContextRegisteredContactId(), targetContactIds, propertyNames, Context.ConnectionAborted);
        }

        public void RemoveSubcriptionProperties(string[] targetContactIds, string[] propertyNames)
        {
            string contextRegisteredContactId = GetContextRegisteredContactId(throwIfNotFound: false);
            if (!string.IsNullOrEmpty(contextRegisteredContactId))
            {
                this.presenceService.RemoveSubcriptionProperties(Context.ConnectionId, contextRegisteredContactId, targetContactIds, propertyNames);
            }
        }

        public void RemoveSubscription(string[] targetContactIds)
        {
            string contextRegisteredContactId = GetContextRegisteredContactId(throwIfNotFound: false);
            if (!string.IsNullOrEmpty(contextRegisteredContactId))
            {
                this.presenceService.RemoveSubscription(Context.ConnectionId, contextRegisteredContactId, targetContactIds);
            }
        }

        public Task SendMessageAsync(string targetContactId, string messageType, JToken body)
        {
            return this.presenceService.SendMessageAsync(GetContextRegisteredContactId(), targetContactId, messageType, body, Context.ConnectionAborted);
        }

        public async Task UnregisterSelfContactAsync()
        {
            string contextRegisteredContactId = GetContextRegisteredContactId(throwIfNotFound: false);
            if (!string.IsNullOrEmpty(contextRegisteredContactId))
            {
                await this.presenceService.UnregisterSelfContactAsync(
                    Context.ConnectionId,
                    contextRegisteredContactId,
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
            string contextRegisteredContactId = GetContextRegisteredContactId(throwIfNotFound: false);
            if (!string.IsNullOrEmpty(contextRegisteredContactId))
            {
                this.logger.LogDebug($"OnDisconnectedAsync contextRegisteredContactId:{contextRegisteredContactId}");
                await this.presenceService.UnregisterSelfContactAsync(
                    Context.ConnectionId,
                    contextRegisteredContactId,
                    (properties) => Task.Delay(TimeSpan.FromSeconds(DisconnectSubscriptionDelaySecs)),
                    Context.ConnectionAborted);
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

        // Note: next methods will prevent errors (method not found) when old presence clients connect to this service
        #region Deprecated Methods

        public Task<Dictionary<string, Dictionary<string, object>>[]> MatchMultipleContacts(Dictionary<string, object>[] matchingProperties)
        {
            return MatchContactsAsync(matchingProperties);
        }

        public Task<Dictionary<string, Dictionary<string, object>>> AddSubcriptions(string[] targetContactIds, string[] propertyNames)
        {
            return AddSubcriptionsAsync(targetContactIds, propertyNames);
        }

        public void RemoveSubcriptions(string[] targetContactIds, string[] propertyNames)
        {
            RemoveSubcriptionProperties(targetContactIds, propertyNames);
        }

        public void RemoveAllSubcriptions(string[] targetContactIds)
        {
            RemoveSubscription(targetContactIds);
        }

        public Task<Dictionary<string, Dictionary<string, object>>> SearchContacts(Dictionary<string, SearchProperty> searchProperties, int? maxCount)
        {
            return SearchContactsAsync(searchProperties, maxCount);
        }

        #endregion

        private string GetContextRegisteredContactId(bool throwIfNotFound = true)
        {
            object registeredContactId;
            if (!Context.Items.TryGetValue(ContactIdKey, out registeredContactId) && throwIfNotFound)
            {
                throw new InvalidOperationException($"No context contact id found for connection id:{Context.ConnectionId}");
            }

            return registeredContactId?.ToString();
        }

    }
}
