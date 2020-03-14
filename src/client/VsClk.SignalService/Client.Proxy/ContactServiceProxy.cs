// <copyright file="ContactServiceProxy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// The presence service proxy client that connects to the remote presence Hub.
    /// </summary>
    public class ContactServiceProxy : ProxyBase, IContactServiceProxy
    {
        /// <summary>
        /// Name of the remote hub.
        /// </summary>
        public const string HubName = "presenceServiceHub";

        /// <summary>
        /// Initializes a new instance of the <see cref="ContactServiceProxy"/> class.
        /// </summary>
        /// <param name="hubProxy">The hub proxy instance.</param>
        /// <param name="trace">Trace instance.</param>
        public ContactServiceProxy(IHubProxy hubProxy, TraceSource trace)
            : this(hubProxy, trace, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContactServiceProxy"/> class.
        /// </summary>
        /// <param name="hubProxy">The hub proxy instance.</param>
        /// <param name="trace">Trace instance.</param>
        /// <param name="formatProvider">Optional format provider.</param>
        public ContactServiceProxy(IHubProxy hubProxy, TraceSource trace, IFormatProvider formatProvider)
            : base(hubProxy, trace, formatProvider)
        {
            AddHubHandler(hubProxy.On(
                ContactHubMethods.UpdateValues,
                new Type[] { typeof(ContactReference), typeof(Dictionary<string, object>), typeof(string) },
                (args) =>
            {
                var contact = (ContactReference)args[0];
                var properties = ToProxyDictionary(args[1]);
                var targetConnectionId = (string)args[2];

                trace.Verbose($"UpdateProperties-> contact:{ToString(contact)} properties:{properties.ConvertToString(FormatProvider)}");
                UpdateProperties?.Invoke(this, new UpdatePropertiesEventArgs(contact, properties, targetConnectionId));
                return Task.CompletedTask;
            }));

            AddHubHandler(hubProxy.On(
                ContactHubMethods.ReceiveMessage,
                new Type[] { typeof(ContactReference), typeof(ContactReference), typeof(string), typeof(object) },
                (args) =>
            {
                var targetContact = (ContactReference)args[0];
                var fromContact = (ContactReference)args[1];
                var messageType = (string)args[2];
                var body = args[3];
                trace.Verbose($"MessageReceived-> targetContact:{ToString(targetContact)} fromContact:{ToString(fromContact)} messageType:{messageType} body:{body:K}");
                MessageReceived?.Invoke(this, new ReceiveMessageEventArgs(targetContact, fromContact, messageType, body));
                return Task.CompletedTask;
            }));

            AddHubHandler(hubProxy.On(
                ContactHubMethods.ConnectionChanged,
                new Type[] { typeof(ContactReference), typeof(ConnectionChangeType) },
                (args) =>
            {
                var contact = (ContactReference)args[0];
                var changeType = ToProxyEnum<ConnectionChangeType>(args[1]);
                trace.Verbose($"ConnectionChanged-> contact:{ToString(contact)} changeType:{changeType}");
                ConnectionChanged?.Invoke(this, new ConnectionChangedEventArgs(contact, changeType));
                return Task.CompletedTask;
            }));
        }

        /// <inheritdoc/>
        public event EventHandler<UpdatePropertiesEventArgs> UpdateProperties;

        /// <inheritdoc/>
        public event EventHandler<ReceiveMessageEventArgs> MessageReceived;

        /// <inheritdoc/>
        public event EventHandler<ConnectionChangedEventArgs> ConnectionChanged;

        /// <inheritdoc/>
        public async Task<Dictionary<string, Dictionary<string, PropertyValue>>> GetSelfConnectionsAsync(string contactId, CancellationToken cancellationToken)
        {
            var result = await HubProxy.InvokeAsync<Dictionary<string, Dictionary<string, PropertyValue>>>(nameof(IContactServiceHub.GetSelfConnectionsAsync), new object[] { contactId }, cancellationToken);
            return result;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, object>> RegisterSelfContactAsync(string contactId, Dictionary<string, object> initialProperties, CancellationToken cancellationToken)
        {
            var registerProperties = await HubProxy.InvokeAsync<Dictionary<string, object>>(nameof(IContactServiceHub.RegisterSelfContactAsync), new object[] { contactId, initialProperties }, cancellationToken);
            return registerProperties;
        }

        /// <inheritdoc/>
        public Task PublishPropertiesAsync(Dictionary<string, object> updateProperties, CancellationToken cancellationToken)
        {
            return HubProxy.InvokeAsync<object>(nameof(IContactServiceHub.PublishPropertiesAsync), new object[] { updateProperties }, cancellationToken);
        }

        /// <inheritdoc/>
        public Task SendMessageAsync(ContactReference targetContact, string messageType, object body, CancellationToken cancellationToken)
        {
            return HubProxy.InvokeAsync<object>(nameof(IContactServiceHub.SendMessageAsync), new object[] { targetContact, messageType, body }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, Dictionary<string, object>>> AddSubcriptionsAsync(ContactReference[] targetContacts, string[] propertyNames, CancellationToken cancellationToken)
        {
            var result = await HubProxy.InvokeAsync<Dictionary<string, Dictionary<string, object>>>(nameof(IContactServiceHub.AddSubcriptionsAsync), new object[] { targetContacts, propertyNames }, cancellationToken);
            return result.ToDictionary(kvp => kvp.Key, kvp => UnboxProperties(kvp.Value));
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, object>[]> RequestSubcriptionsAsync(Dictionary<string, object>[] targetContactProperties, string[] propertyNames, bool useStubContact, CancellationToken cancellationToken)
        {
            var result = await HubProxy.InvokeAsync<Dictionary<string, object>[]>(nameof(IContactServiceHub.RequestSubcriptionsAsync), new object[] { targetContactProperties, propertyNames, useStubContact }, cancellationToken);
            return result.Select(i => UnboxProperties(i)).ToArray();
        }

        /// <inheritdoc/>
        public Task RemoveSubscriptionAsync(ContactReference[] targetContacts, CancellationToken cancellationToken)
        {
            return HubProxy.InvokeAsync<object>(nameof(IContactServiceHub.RemoveSubscription), new object[] { targetContacts }, cancellationToken);
        }

        /// <inheritdoc/>
        public Task UnregisterSelfContactAsync(CancellationToken cancellationToken)
        {
            return HubProxy.InvokeAsync<object>(nameof(IContactServiceHub.UnregisterSelfContactAsync), new object[] { }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, Dictionary<string, object>>[]> MatchContactsAsync(Dictionary<string, object>[] matchingProperties, CancellationToken cancellationToken)
        {
            var results = await HubProxy.InvokeAsync<Dictionary<string, Dictionary<string, object>>[]>(nameof(IContactServiceHub.MatchContactsAsync), matchingProperties, cancellationToken);
            return results.Select(item => item.ToDictionary(kvp => kvp.Key, kvp => UnboxProperties(kvp.Value))).ToArray();
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, Dictionary<string, object>>> SearchContactsAsync(Dictionary<string, SearchProperty> searchProperties, int? maxCount, CancellationToken cancellationToken)
        {
            var result = await HubProxy.InvokeAsync<Dictionary<string, Dictionary<string, object>>>(nameof(IContactServiceHub.SearchContactsAsync), new object[] { searchProperties, maxCount }, cancellationToken);
            return result.ToDictionary(kvp => kvp.Key, kvp => UnboxProperties(kvp.Value));
        }

        private static Dictionary<string, object> UnboxProperties(Dictionary<string, object> properties)
        {
            return properties.ToDictionary(kvp => kvp.Key, kvp => NewtonsoftHelpers.ToObject(kvp.Value));
        }

        private string ToString(ContactReference contactReference)
        {
            return contactReference.ToString(FormatProvider);
        }
    }
}
