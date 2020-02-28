// <copyright file="ContactBackplaneServiceProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;
using ContactDataInfo = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>>;

namespace Microsoft.VsCloudKernel.SignalService
{
    public class ContactBackplaneServiceProvider : BackplaneServiceProviderBase, IContactBackplaneProvider
    {
        public ContactBackplaneServiceProvider(
            IBackplaneConnectorProvider backplaneConnectorProvider,
            string hostServiceId,
            CancellationToken stoppingToken)
            : base(backplaneConnectorProvider, hostServiceId, stoppingToken)
        {
            Func<ContactDataChanged<ContactDataInfo>, string[], CancellationToken, Task> onUpdateCallback = (contactDataChanged, affectedProperties, ct) =>
            {
                return FireOnUpdateContactAsync(contactDataChanged, affectedProperties, ct);
            };
            backplaneConnectorProvider.AddTarget(nameof(FireOnUpdateContactAsync), onUpdateCallback);

            Func<string, MessageData, CancellationToken, Task> onSendMessageCallback = (sourceId, messageData, ct) =>
            {
                return FireOnSendMessageAsync(sourceId, messageData, ct);
            };
            backplaneConnectorProvider.AddTarget(nameof(FireOnSendMessageAsync), onSendMessageCallback);
        }

        /// <inheritdoc/>
        public OnContactChangedAsync ContactChangedAsync { get; set; }

        /// <inheritdoc/>
        public OnMessageReceivedAsync MessageReceivedAsync { get; set; }

        /// <inheritdoc/>
        protected override string ServiceType => "contacts";

        /// <inheritdoc/>
        public async Task<Dictionary<string, ContactDataInfo>[]> GetContactsDataAsync(Dictionary<string, object>[] matchProperties, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            var result = await BackplaneConnectorProvider.InvokeAsync<Dictionary<string, ContactDataInfo>[]>(nameof(GetContactsDataAsync), new object[] { matchProperties }, cancellationToken);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            var result = await BackplaneConnectorProvider.InvokeAsync<ContactDataInfo>(nameof(GetContactDataAsync), new object[] { contactId }, cancellationToken);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ContactDataInfo> UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            var result = await BackplaneConnectorProvider.InvokeAsync<ContactDataInfo>(nameof(UpdateContactAsync), new object[] { contactDataChanged }, cancellationToken);
            return result;
        }

        /// <inheritdoc/>
        public async Task SendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            await BackplaneConnectorProvider.InvokeAsync<object>(nameof(SendMessageAsync), new object[] { sourceId, messageData }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateMetricsAsync((string ServiceId, string Stamp) serviceInfo, ContactServiceMetrics metrics, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            await BackplaneConnectorProvider.InvokeAsync<object>(nameof(UpdateMetricsAsync), new object[] { serviceInfo, metrics }, cancellationToken);
        }

        /// <inheritdoc/>
        public Task DisposeDataChangesAsync(DataChanged[] dataChanges, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public bool HandleException(string methodName, Exception error) => false;

        public Task FireOnUpdateContactAsync(ContactDataChanged<ContactDataInfo> contactDataChanged, string[] affectedProperties, CancellationToken cancellationToken)
        {
            return ContactChangedAsync?.Invoke(contactDataChanged, affectedProperties, cancellationToken);
        }

        public Task FireOnSendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
        {
            return MessageReceivedAsync?.Invoke(sourceId, messageData, cancellationToken);
        }
    }
}
