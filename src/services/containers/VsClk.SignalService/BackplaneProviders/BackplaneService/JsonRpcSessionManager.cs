using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Common;
using StreamJsonRpc;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// Class to manage json rpc sessions 
    /// </summary>
    public class JsonRpcSessionManager : IContactBackplaneServiceNotification
    {
        private readonly ConcurrentHashSet<JsonRpc> rpcSessions = new ConcurrentHashSet<JsonRpc>();

        public JsonRpcSessionManager(ContactBackplaneService backplaneService, ILogger<JsonRpcSessionManager> logger)
        {
            BackplaneService = Requires.NotNull(backplaneService, nameof(backplaneService));
            Logger = Requires.NotNull(logger, nameof(logger));
            backplaneService.AddContactBackplaneServiceNotification(this);
        }

        private ILogger Logger { get; }
        private ContactBackplaneService BackplaneService { get; }

        internal void StartSession(Stream stream)
        {
            var jsonRpc = JsonRpc.Attach(stream, this);
            this.rpcSessions.Add(jsonRpc);

            Logger.LogMethodScope(LogLevel.Information, $"connections:{this.rpcSessions.Count}", nameof(StartSession));

            jsonRpc.Disconnected += (s, e) =>
            {
                BackplaneService.OnDisconnected(null, e.Exception);
                this.rpcSessions.TryRemove(jsonRpc);
                Logger.LogInformation($"rpc disonnected -> connections:{this.rpcSessions.Count}");
            };
        }

        #region IContactBackplaneServiceNotification

        async Task IContactBackplaneServiceNotification.FireOnUpdateContactAsync(ContactDataChanged<ContactDataInfo> contactDataChanged, string[] affectedProperties, CancellationToken cancellationToken)
        {
            foreach(var jsonRpc in this.rpcSessions.Values)
            {
                try
                {
                    await jsonRpc.NotifyAsync(nameof(IContactBackplaneServiceNotification.FireOnUpdateContactAsync), contactDataChanged, affectedProperties);
                }
                catch (Exception err)
                {
                    Logger.LogError(err, "Failed to notify->FireOnUpdateContactAsync");
                }
            }
        }

        async Task IContactBackplaneServiceNotification.FireOnSendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
        {
            foreach (var jsonRpc in this.rpcSessions.Values)
            {
                try
                {
                    await jsonRpc.NotifyAsync(nameof(IContactBackplaneServiceNotification.FireOnSendMessageAsync), sourceId, messageData);
                }
                catch (Exception err)
                {
                    Logger.LogError(err, "Failed to notify->FireOnSendMessageAsync");
                }
            }
        }

        #endregion

        public Task RegisterService(string serviceId)
        {
            BackplaneService.RegisterService(serviceId);
            return Task.CompletedTask;
        }

        public Task UpdateMetricsAsync((string ServiceId, string Stamp) serviceInfo, ContactServiceMetrics metrics, CancellationToken cancellationToken) =>
            BackplaneService.UpdateMetricsAsync(serviceInfo, metrics, cancellationToken);

        public Task UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken) =>
            BackplaneService.UpdateContactAsync(contactDataChanged, cancellationToken);

        public Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken) =>
            BackplaneService.GetContactDataAsync(contactId, cancellationToken);

        public Task<Dictionary<string, ContactDataInfo>[]> GetContactsDataAsync(Dictionary<string, object>[] matchProperties, CancellationToken cancellationToken) =>
            BackplaneService.GetContactsDataAsync(matchProperties, cancellationToken);

        public Task SendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken) =>
            BackplaneService.SendMessageAsync(sourceId, messageData, cancellationToken);
    }
}
