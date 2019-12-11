using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Microsoft.VsCloudKernel.SignalService
{
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// Implement IContactBackplaneProvider based on a json rpc channel
    /// </summary>
    public class JsonRpcContactBackplaneServiceProvider : ContactBackplaneServiceProviderBase
    {
        private const int JsonPort = 3150;
        private JsonRpc jsonRpc;

        private readonly string host;

        private JsonRpcContactBackplaneServiceProvider(string host, ILogger logger, string hostServiceId, CancellationToken stoppingToken)
            : base(logger, hostServiceId, stoppingToken)
        {
            Requires.NotNullOrEmpty(host, nameof(host));
            this.host = host;
        }

        public static async Task<JsonRpcContactBackplaneServiceProvider> CreateAsync(string host, ILogger logger, string hostServiceId, CancellationToken stoppingToken)
        {
            logger.LogInformation($"Creating jsonrpc backplane service using host:{host}");

            var instance = new JsonRpcContactBackplaneServiceProvider(host, logger, hostServiceId, stoppingToken);
            await instance.AttemptConnectAsync(stoppingToken);
            return instance;
        }

        #region Overrides

        protected override bool IsConnected => this.jsonRpc != null;

        protected override async Task AttemptConnectInternalAsync(CancellationToken cancellationToken)
        {
            var retries = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    Logger.LogDebug($"ConnectAsync -> retries:{retries}");
                    var tcpStream = await ConnectAsync(this.host, JsonPort, null, cancellationToken);
                    Attach(tcpStream);

                    Logger.LogDebug($"Succesfully connected...");
                    break;
                }
                catch (Exception err)
                {
                    ++retries;
                    Logger.LogDebug($"Failed to connect-> name:{err.GetType().Name} err:{err.Message}");
                    await Task.Delay(2000, cancellationToken);
                }
            }

            await this.jsonRpc.InvokeWithCancellationAsync("RegisterService", new object[] { HostServiceId }, cancellationToken);
        }

        protected override Task<JArray> GetContactsDataInternalAsync(Dictionary<string, object>[] matchProperties, CancellationToken cancellationToken)
        {
            return this.jsonRpc.InvokeWithCancellationAsync<JArray>(nameof(GetContactsDataAsync), new object[] { matchProperties }, cancellationToken);
        }

        protected override Task<JObject> GetContactDataInternalAsync(string contactId, CancellationToken cancellationToken)
        {
            return this.jsonRpc.InvokeWithCancellationAsync<JObject>(nameof(GetContactDataAsync), new object[] { contactId }, cancellationToken);
        }

        protected override Task<JObject> UpdateContactInternalAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken)
        {
            return this.jsonRpc.InvokeWithCancellationAsync<JObject>(nameof(UpdateContactAsync), new object[] { contactDataChanged }, cancellationToken);
        }

        protected override Task SendMessageInternalAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
        {
            return this.jsonRpc.InvokeWithCancellationAsync(nameof(SendMessageAsync), new object[] { sourceId, messageData }, cancellationToken);
        }

        protected override Task UpdateMetricsInternalAsync((string ServiceId, string Stamp) serviceInfo, ContactServiceMetrics metrics, CancellationToken cancellationToken)
        {
            return this.jsonRpc.InvokeWithCancellationAsync(nameof(UpdateMetricsAsync), new object[] { serviceInfo, metrics }, cancellationToken);
        }

        #endregion

        public Task FireOnUpdateContactAsync(ContactDataChanged<ContactDataInfo> contactDataChanged, string[] affectedProperties, CancellationToken cancellationToken)
        {
            return ContactChangedAsync?.Invoke(contactDataChanged, affectedProperties, cancellationToken);
        }

        public Task FireOnSendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
        {
            return MessageReceivedAsync?.Invoke(sourceId, messageData, cancellationToken);
        }

        private void Attach(Stream tcpStream)
        {
            this.jsonRpc = JsonRpc.Attach(tcpStream, this);
            this.jsonRpc.Disconnected += (s, e) =>
            {
                Logger.LogError(e.Exception, $"Disconnected reason:{e.Reason}");
                this.jsonRpc = null;
                Task.Run(() => AttemptConnectAsync(StoppingToken)).Forget();
            };
        }

        private static async Task<Stream> ConnectAsync(string host,int port, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            TcpClient client;
            if (IPAddress.TryParse(host, out IPAddress ipAddress))
            {
                client = new TcpClient(ipAddress.AddressFamily);
            }
            else
            {
                client = new TcpClient();
            }

            //// We may expect immediate responses from each packet we send, so disable a delay when send or receive buffers are not full.
            //client.NoDelay = DisableTcpDelay;

            Action cancel = () =>
            {
                if (!client.Connected)
                {
                    // Close the underlying Socket, rather than closing the TcpClient.
                    // This avoids a bug in TcpClient where it can throw a
                    // NullReferenceException if it's closed too early.
                    client.Client.Close();
                }
            };

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(cancel);
            }

            var timeoutSource = new CancellationTokenSource();
            if (timeout.HasValue)
            {
                timeoutSource.Token.Register(cancel);
                timeoutSource.CancelAfter(timeout.Value);
            }

            try
            {
                if (ipAddress != null)
                {
                    await client.ConnectAsync(ipAddress, port);
                }
                else
                {
                    await client.ConnectAsync(host, port);
                }
            }
            catch (ObjectDisposedException)
            {
                if (timeoutSource.IsCancellationRequested)
                {
                    throw new TimeoutException();
                }

                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }

            return client.GetStream();
        }
    }
}
