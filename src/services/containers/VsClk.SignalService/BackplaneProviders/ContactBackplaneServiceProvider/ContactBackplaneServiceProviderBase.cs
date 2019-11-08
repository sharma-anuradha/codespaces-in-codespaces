using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    public abstract class ContactBackplaneServiceProviderBase : IContactBackplaneProvider
    {
        private const int TimeoutConnectionMillisecs = 2000;

        private TaskCompletionSource<bool> connectedTcs;

        protected ILogger Logger { get; }
        protected CancellationToken StoppingToken { get; }
        protected string HostServiceId { get; }
        private Task ConnectedTask => this.connectedTcs.Task;

        protected abstract bool IsConnected { get; }

        protected ContactBackplaneServiceProviderBase(ILogger logger, string hostServiceId, CancellationToken stoppingToken)
        {
            HostServiceId = hostServiceId;
            Logger = Requires.NotNull(logger, nameof(logger));
            StoppingToken = stoppingToken;
        }

        private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            if (!IsConnected)
            {
                if (await Task.WhenAny(ConnectedTask, Task.Delay(TimeoutConnectionMillisecs, cancellationToken)) != ConnectedTask)
                {
                    throw new TimeoutException("Waiting to connect on backplane server");
                }
            }
        }

        #region IBackplaneProvider

        public OnContactChangedAsync ContactChangedAsync { get; set; }

        public OnMessageReceivedAsync MessageReceivedAsync { get; set; }

        public int Priority => 200;

        public async Task<Dictionary<string, ContactDataInfo>> GetContactsDataAsync(Dictionary<string, object> matchProperties, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            var jObject = await GetContactsDataInternalAsync(matchProperties, cancellationToken);
            return ((IDictionary<string, JToken>)jObject).ToDictionary(kvp => kvp.Key, kvp => ToContactDataInfo((JObject)kvp.Value));
        }

        public async Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            var jObject = await GetContactDataInternalAsync(contactId, cancellationToken);
            return jObject != null ? ToContactDataInfo(jObject) : null;
        }

        public async Task<ContactDataInfo> UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            var jObject = await UpdateContactInternalAsync(contactDataChanged, cancellationToken);
            return jObject != null ? ToContactDataInfo(jObject) : null;
        }

        public async Task SendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            await SendMessageInternalAsync(sourceId, messageData, cancellationToken);
        }

        public async Task UpdateMetricsAsync((string ServiceId, string Stamp) serviceInfo, ContactServiceMetrics metrics, CancellationToken cancellationToken)
        {
            await EnsureConnectedAsync(cancellationToken);
            await UpdateMetricsInternalAsync(serviceInfo, metrics, cancellationToken);
        }

        public Task DisposeDataChangesAsync(DataChanged[] dataChanges, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public bool HandleException(string methodName, Exception error) => false;

        #endregion

        protected async Task AttemptConnectAsync(CancellationToken cancellationToken)
        {
            this.connectedTcs = new TaskCompletionSource<bool>();
            await AttemptConnectInternalAsync(cancellationToken);
            this.connectedTcs.TrySetResult(true);
        }

        protected abstract Task AttemptConnectInternalAsync(CancellationToken cancellationToken);

        protected abstract Task<JObject> GetContactsDataInternalAsync(Dictionary<string, object> matchProperties, CancellationToken cancellationToken);
        protected abstract Task<JObject> GetContactDataInternalAsync(string contactId, CancellationToken cancellationToken);
        protected abstract Task<JObject> UpdateContactInternalAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken);
        protected abstract Task SendMessageInternalAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken);
        protected abstract Task UpdateMetricsInternalAsync((string ServiceId, string Stamp) serviceInfo, ContactServiceMetrics metrics, CancellationToken cancellationToken);

        private static IDictionary<string, IDictionary<string, PropertyValue>> ToConnectionProperties(JObject jObject)
        {
            return ((IDictionary<string, JToken>)jObject).ToDictionary(
                kvp => kvp.Key,
                kvp => (IDictionary<string, PropertyValue>)((IDictionary<string, JToken>)kvp.Value).ToDictionary(kvp2 => kvp2.Key, kvp2 => kvp2.Value.ToObject<PropertyValue>()));
        }

        private static ContactDataInfo ToContactDataInfo(JObject jObject)
        {
            return ((IDictionary<string, JToken>)jObject).ToDictionary(kvp => kvp.Key, kvp => ToConnectionProperties((JObject)kvp.Value));
        }

    }
}
