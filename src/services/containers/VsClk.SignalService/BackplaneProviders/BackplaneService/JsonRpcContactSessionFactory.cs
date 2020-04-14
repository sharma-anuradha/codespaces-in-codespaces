// <copyright file="JsonRpcContactSessionFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Common;
using StreamJsonRpc;
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;
using ContactDataInfo = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>>;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// Json Rpc contact service session manager.
    /// </summary>
    public class JsonRpcContactSessionFactory : JsonRpcSessionFactory<ContactBackplaneService, IContactBackplaneManager,  IContactBackplaneServiceNotification>, IContactBackplaneServiceNotification
    {
        private readonly ConcurrentHashSet<JsonRpc> rpcSessions = new ConcurrentHashSet<JsonRpc>();

        public JsonRpcContactSessionFactory(ContactBackplaneService backplaneService, ILogger<JsonRpcContactSessionFactory> logger)
            : base(backplaneService, logger)
        {
        }

        /// <inheritdoc/>
        public override string ServiceType => "contacts";

        /// <inheritdoc/>
        Task IContactBackplaneServiceNotification.FireOnUpdateContactAsync(ContactDataChanged<ContactDataInfo> contactDataChanged, string[] affectedProperties, CancellationToken cancellationToken)
        {
            return InvokeAllAsync(nameof(IContactBackplaneServiceNotification.FireOnUpdateContactAsync), contactDataChanged, affectedProperties);
        }

        /// <inheritdoc/>
        Task IContactBackplaneServiceNotification.FireOnSendMessageAsync(MessageData messageData, CancellationToken cancellationToken)
        {
            return InvokeAllAsync(nameof(IContactBackplaneServiceNotification.FireOnSendMessageAsync), messageData);
        }

        public Task UpdateMetricsAsync((string ServiceId, string Stamp) serviceInfo, ContactServiceMetrics metrics, CancellationToken cancellationToken) =>
            BackplaneService.UpdateMetricsAsync(serviceInfo, metrics, cancellationToken);

        public Task UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken) =>
            BackplaneService.UpdateContactAsync(contactDataChanged, cancellationToken);

        public Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken) =>
            BackplaneService.GetContactDataAsync(contactId, cancellationToken);

        public Task<Dictionary<string, ContactDataInfo>[]> GetContactsDataAsync(Dictionary<string, object>[] matchProperties, CancellationToken cancellationToken) =>
            BackplaneService.GetContactsDataAsync(matchProperties, cancellationToken);

        public Task SendMessageAsync(MessageData messageData, CancellationToken cancellationToken) =>
            BackplaneService.SendMessageAsync(messageData, cancellationToken);
    }
}
