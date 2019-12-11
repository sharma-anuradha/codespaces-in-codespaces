using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Client;
using Microsoft.VsCloudKernel.SignalService.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService
{
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// A provider that use a backplane ASP.NET Core service that is running in the local network
    /// </summary>
    public class HubContactBackplaneServiceProvider : ContactBackplaneServiceProviderBase
    {
        private bool creatingFlag = true;

        private HubContactBackplaneServiceProvider(HubConnection hubConnection, ILogger logger, IDataFormatProvider serviceFormatProvider, string hostServiceId, CancellationToken stoppingToken)
            : base(logger, hostServiceId, stoppingToken)
        {
            // Create a logger and a listener to report into ASP.NET Core
            Trace = new TraceSource(nameof(HubContactBackplaneServiceProvider));
            Trace.Switch.Level = SourceLevels.All;
            Trace.Listeners.Add(new LoggerAdapterListener(LoggerCallback, serviceFormatProvider));

            HubConnection = hubConnection;
            hubConnection.Closed += OnClosedAsync;

            hubConnection.On<ContactDataChanged<ContactDataInfo>, string[]>("OnUpdateContact", (contactdDataInfo, affectedProperties) =>
             {
                ContactChangedAsync?.Invoke(contactdDataInfo, affectedProperties, StoppingToken);
             });

            hubConnection.On<string, MessageData>("OnSendMessage", (serviceId, messageData) =>
            {
                MessageReceivedAsync?.Invoke(serviceId, messageData, StoppingToken);
            });
        }

        private TraceSource Trace { get; }

        private HubConnection HubConnection { get; }


        public static async Task<HubContactBackplaneServiceProvider> CreateAsync(string url, ILogger logger, IDataFormatProvider serviceFormatProvider, string hostServiceId, CancellationToken stoppingToken)
        {
            logger.LogInformation($"Creating hub backplane service using url:{url}");

            var hubConnection = new HubConnectionBuilder().WithUrl(url, options =>
            {
                // placeholer to define 
            }).ConfigureLogging(logging =>
            {
                // Log to the Console
                logging.AddProvider(new LoggerProviderFactory(logger));

                // This will set ALL logging to Information level
                logging.SetMinimumLevel(LogLevel.Information);
            }).Build();
            // increase sever timeout
            hubConnection.ServerTimeout = TimeSpan.FromMinutes(2);

            var instance = new HubContactBackplaneServiceProvider(hubConnection, logger, serviceFormatProvider, hostServiceId, stoppingToken);
            await instance.AttemptConnectAsync(stoppingToken);
            instance.creatingFlag = false;
            return instance;
        }

        #region overrides

        protected override bool IsConnected => HubConnection.State == HubConnectionState.Connected;

        protected override async Task AttemptConnectInternalAsync(CancellationToken cancellationToken)
        {
            await HubConnection.ConnectAsync(
               (retries, backoffTime, error) =>
               {
                   return Task.FromResult(backoffTime);
               },
               -1,
               2000,
               10000,
               Trace,
               cancellationToken);
            await HubConnection.InvokeAsync("RegisterService", HostServiceId, cancellationToken);
        }


        protected override Task<JArray> GetContactsDataInternalAsync(Dictionary<string, object>[] matchProperties, CancellationToken cancellationToken)
        {
            return HubConnection.InvokeAsync<JArray>(nameof(GetContactsDataAsync), matchProperties, cancellationToken);
        }

        protected override Task<JObject> GetContactDataInternalAsync(string contactId, CancellationToken cancellationToken)
        {
            return HubConnection.InvokeAsync<JObject>(nameof(GetContactDataAsync), contactId, cancellationToken);
        }

        protected override Task<JObject> UpdateContactInternalAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken)
        {
            return HubConnection.InvokeAsync<JObject>(nameof(UpdateContactAsync), contactDataChanged, cancellationToken);
        }

        protected override Task SendMessageInternalAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
        {
            return HubConnection.InvokeAsync(nameof(SendMessageAsync), sourceId, messageData, cancellationToken);
        }

        protected override Task UpdateMetricsInternalAsync((string ServiceId, string Stamp) serviceInfo, ContactServiceMetrics metrics, CancellationToken cancellationToken)
        {
            return HubConnection.InvokeAsync(nameof(UpdateMetricsAsync), serviceInfo, metrics, cancellationToken);
        }

        #endregion

        private static LogLevel ToLogLevel(TraceEventType eventType)
        {
            switch (eventType)
            {
                case TraceEventType.Verbose:
                    return LogLevel.Debug;
                case TraceEventType.Information:
                    return LogLevel.Information;
                case TraceEventType.Critical:
                    return LogLevel.Critical;
                case TraceEventType.Warning:
                    return LogLevel.Warning;
                case TraceEventType.Error:
                default:
                    return LogLevel.Error;
            }
        }

        private void LoggerCallback(TraceEventType traceEventType, string message)
        {
            Logger.Log(this.creatingFlag && traceEventType == TraceEventType.Error ? LogLevel.Debug : ToLogLevel(traceEventType), message);
        }

        private async Task OnClosedAsync(Exception exception)
        {
            Trace.Info($"OnClosedAsync exception:{exception?.Message}");
            await AttemptConnectAsync(StoppingToken);
        }

        private class LoggerProviderFactory : ILoggerProvider
        {
            private readonly ILogger logger;

            public LoggerProviderFactory(ILogger logger)
            {
                this.logger = logger;
            }

            public ILogger CreateLogger(string categoryName) => this.logger;

            public void Dispose()
            {
            }
        }
    }
}
