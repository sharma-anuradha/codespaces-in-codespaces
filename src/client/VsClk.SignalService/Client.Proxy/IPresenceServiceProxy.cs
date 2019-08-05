// <copyright file="IPresenceServiceProxy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// The client presence service proxy.
    /// </summary>
    public interface IPresenceServiceProxy
    {
        /// <summary>
        /// When update properties changed
        /// </summary>
        event EventHandler<UpdatePropertiesEventArgs> UpdateProperties;

        /// <summary>
        /// When a message is received.
        /// </summary>
        event EventHandler<ReceiveMessageEventArgs> MessageReceived;

        /// <summary>
        /// When a connection has changed.
        /// </summary>
        event EventHandler<ConnectionChangedEventArgs> ConnectionChanged;

        /// <summary>
        /// Get the self connections of a contact.
        /// </summary>
        /// <param name="contactId">The contact id to query.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Map of all the conenctiosn associated with the contact.</returns>
        Task<Dictionary<string, Dictionary<string, PropertyValue>>> GetSelfConnectionsAsync(string contactId, CancellationToken cancellationToken);

        /// <summary>
        /// Register a self contact.
        /// </summary>
        /// <param name="contactId">The contact id to regsiter.</param>
        /// <param name="initialProperties">Initial properties to register.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>List of reserved properties from the server.</returns>
        Task<Dictionary<string, object>> RegisterSelfContactAsync(string contactId, Dictionary<string, object> initialProperties, CancellationToken cancellationToken);

        /// <summary>
        /// Publish properties on the registered contact.
        /// </summary>
        /// <param name="updateProperties">The new properties.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task completion</returns>
        Task PublishPropertiesAsync(Dictionary<string, object> updateProperties, CancellationToken cancellationToken);

        /// <summary>
        /// Send a message to another contact.
        /// </summary>
        /// <param name="targetContact">The target cintact to send the message.</param>
        /// <param name="messageType">Message type.</param>
        /// <param name="body">Body of the message to send.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task completion</returns>
        Task SendMessageAsync(ContactReference targetContact, string messageType, object body, CancellationToken cancellationToken);

        /// <summary>
        /// Add a set subscriptions.
        /// </summary>
        /// <param name="targetContacts">Which target contacts we want a subscription.</param>
        /// <param name="propertyNames">Property names to subscribe.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The existing published values for the susbcribed contacts.</returns>
        Task<Dictionary<string, Dictionary<string, object>>> AddSubcriptionsAsync(ContactReference[] targetContacts, string[] propertyNames, CancellationToken cancellationToken);

        /// <summary>
        /// Request a subscription based on matching target properties.
        /// </summary>
        /// <param name="targetContactProperties">The target contact properties to match.</param>
        /// <param name="propertyNames">Property names to subscribe.</param>
        /// <param name="useStubContact">If using a stub ocntact in case no slef contact is found.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The existing published values for the susbcribed contacts.</returns>
        Task<Dictionary<string, object>[]> RequestSubcriptionsAsync(Dictionary<string, object>[] targetContactProperties, string[] propertyNames, bool useStubContact, CancellationToken cancellationToken);

        /// <summary>
        /// Remove a previous susbscription.
        /// </summary>
        /// <param name="targetContacts">Which contacts we want to remove.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task completion</returns>
        Task RemoveSubscriptionAsync(ContactReference[] targetContacts, CancellationToken cancellationToken);

        /// <summary>
        /// Unregister the self contact.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task completion.</returns>
        Task UnregisterSelfContactAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Match contacts query.
        /// </summary>
        /// <param name="matchingProperties">Which matching properties to query.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Map of matching contacts.</returns>
        Task<Dictionary<string, Dictionary<string, object>>[]> MatchContactsAsync(Dictionary<string, object>[] matchingProperties, CancellationToken cancellationToken);

        /// <summary>
        /// Search contacts using regular expression.
        /// </summary>
        /// <param name="searchProperties">Set of properties to match.</param>
        /// <param name="maxCount">Maximum count to return.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Map of matching contacts.</returns>
        Task<Dictionary<string, Dictionary<string, object>>> SearchContactsAsync(Dictionary<string, SearchProperty> searchProperties, int? maxCount, CancellationToken cancellationToken);
    }
}
