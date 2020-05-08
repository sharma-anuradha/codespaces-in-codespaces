// <copyright file="ContactBackplaneServiceProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;

using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;
using ContactDataInfo = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>>;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// This class is intented to update/receive data into/from a backplane service for multiple
    /// contact services running on the same machine or AKS cluster.
    /// </summary>
    public class ContactBackplaneServiceProvider : BackplaneServiceProviderBase, IContactBackplaneProvider
    {
        public ContactBackplaneServiceProvider(
            IBackplaneConnectorProvider backplaneConnectorProvider,
            string hostServiceId,
            ILogger logger,
            CancellationToken stoppingToken)
            : base(backplaneConnectorProvider, hostServiceId, logger, stoppingToken)
        {
            Func<ContactDataChanged<ContactDataInfo>, string[], CancellationToken, Task> onUpdateCallback = (contactDataChanged, affectedProperties, ct) =>
            {
                return FireOnUpdateContactAsync(contactDataChanged, affectedProperties, ct);
            };
            backplaneConnectorProvider.AddTarget(nameof(FireOnUpdateContactAsync), onUpdateCallback);

            Func<MessageData, CancellationToken, Task> onSendMessageCallback = (messageData, ct) =>
            {
                return FireOnSendMessageAsync(messageData, ct);
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
        public async Task UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            await BackplaneConnectorProvider.SendAsync(nameof(UpdateContactAsync), new object[] { contactDataChanged }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateContactDataInfoAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, ContactDataInfo contactDataInfo, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            await BackplaneConnectorProvider.InvokeAsync<ContactDataInfo>(nameof(UpdateContactDataInfoAsync), new object[] { contactDataChanged, contactDataInfo }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task SendMessageAsync(MessageData messageData, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            await BackplaneConnectorProvider.InvokeAsync<object>(nameof(SendMessageAsync), new object[] { messageData }, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task UpdateMetricsAsync(ServiceInfo serviceInfo, ContactServiceMetrics metrics, CancellationToken cancellationToken)
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

        public async Task FireOnUpdateContactAsync(ContactDataChanged<ContactDataInfo> contactDataChanged, string[] affectedProperties, CancellationToken cancellationToken)
        {
            try
            {
                if (ContactChangedAsync != null)
                {
                    await ContactChangedAsync.Invoke(contactDataChanged, affectedProperties, cancellationToken);
                }
            }
            catch (Exception err)
            {
                Logger.LogMethodScope(
                    LogLevel.Error,
                    err,
                    $"Failed to handle contact update changeId:{contactDataChanged.ChangeId} id:{contactDataChanged.ContactId} type:{contactDataChanged.ChangeType}",
                    nameof(FireOnUpdateContactAsync));
            }
        }

        public async Task FireOnSendMessageAsync(MessageData messageData, CancellationToken cancellationToken)
        {
            try
            {
                if (MessageReceivedAsync != null)
                {
                    await MessageReceivedAsync.Invoke(messageData, cancellationToken);
                }
            }
            catch (Exception err)
            {
                Logger.LogMethodScope(
                    LogLevel.Error,
                    err,
                    $"Failed to handle message payload changeId:{messageData.ChangeId} from:{messageData.FromContact} target:{messageData.TargetContact} type:{messageData.Type}",
                    nameof(FireOnSendMessageAsync));
            }
        }
    }
}
