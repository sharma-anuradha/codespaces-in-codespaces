// <copyright file="HubConnectorProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Client;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// A provider that use a backplane ASP.NET Core signalR service that is running in the local network
    /// </summary>
    public class HubConnectorProvider : IBackplaneConnectorProvider
    {
        private bool creatingFlag = true;

        public HubConnectorProvider(string url, ILogger logger, IDataFormatProvider serviceFormatProvider, string hostServiceId)
        {
            logger.LogInformation($"Creating hub connector provider using url:{url}");

            // Create a logger and a listener to report into ASP.NET Core
            Trace = new TraceSource(nameof(HubConnectorProvider));
            Trace.Switch.Level = SourceLevels.All;
            Trace.Listeners.Add(new LoggerAdapterListener(LoggerCallback, serviceFormatProvider));

            HubConnection = new HubConnectionBuilder().WithUrl(url, options =>
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
            HubConnection.ServerTimeout = TimeSpan.FromMinutes(2);

            // on closed
            HubConnection.Closed += OnClosedAsync;

            Logger = logger;
        }

        /// <inheritdoc/>
        public event EventHandler Disconnected;

        /// <inheritdoc/>
        public bool IsConnected => HubConnection.State == HubConnectionState.Connected;

        private ILogger Logger { get; }

        private TraceSource Trace { get; }

        private HubConnection HubConnection { get; }

        /// <inheritdoc/>
        public async Task AttemptConnectAsync(CancellationToken cancellationToken)
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
               default,
               cancellationToken);
            this.creatingFlag = false;
        }

        /// <inheritdoc/>
        public Task<TResult> InvokeAsync<TResult>(string targetName, object[] arguments, CancellationToken cancellationToken)
        {
            return HubConnection.InvokeCoreAsync<TResult>(targetName, arguments, cancellationToken);
        }

        /// <inheritdoc/>
        public Task SendAsync(string targetName, object[] arguments, CancellationToken cancellationToken)
        {
            return HubConnection.SendCoreAsync(targetName, arguments, cancellationToken);
        }

        /// <inheritdoc/>
        public void AddTarget(string methodName, Delegate handler)
        {
            var parameterTypes = handler.GetMethodInfo().GetParameters().Select(p => p.ParameterType).ToArray();
            HubConnection.On(methodName, parameterTypes, (args) =>
            {
                var task = handler.DynamicInvoke(args) as Task;
                return task;
            });
        }

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

        private Task OnClosedAsync(Exception exception)
        {
            Trace.Info($"OnClosedAsync exception:{exception?.Message}");
            Disconnected?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
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
