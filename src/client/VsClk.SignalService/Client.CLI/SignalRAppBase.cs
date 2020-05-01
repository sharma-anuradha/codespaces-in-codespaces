// <copyright file="SignalRAppBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Client;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace SignalService.Client.CLI
{
    /// <summary>
    /// Base signalR app class.
    /// </summary>
    internal abstract class SignalRAppBase
    {
        private const string SignalRHubName = "signalrhub";
        private const string SignalRHubDevName = "signalrhub-dev";

        private const string DefaultServiceEndpointBase = "http://localhost:5000/";

        private CancellationTokenSource disposeCts = new CancellationTokenSource();

        private Func<string, HubConnection> hubConnectionFactory;

        protected CancellationToken DisposeToken => this.disposeCts.Token;

        protected string HubName { get; private set; }

        protected TraceSource TraceSource { get; private set; }

        protected HubProxyOptions HubProxyOptions { get; private set; }

        public async Task<int> RunAsync(Program cli)
        {
            string serviceEndpoint = cli.ServiceEndpointOption.Value();
            HubProxyOptions = cli.HubName.HasValue() || !IsUniversalHubUri(serviceEndpoint) ? HubProxyOptions.None : HubProxyOptions.UseSignalRHub;
            HubName = cli.HubName.HasValue() ? cli.HubName.Value() : SignalRHubName;
            if (string.IsNullOrEmpty(serviceEndpoint))
            {
                serviceEndpoint = DefaultServiceEndpointBase + HubName;
            }

            this.hubConnectionFactory = (serviceUri) =>
            {
                if (string.IsNullOrEmpty(serviceUri))
                {
                    serviceUri = serviceEndpoint;
                }

                var sb = new StringBuilder($"Create hub connection using uri:{serviceUri}");

                var hubConnectionBuilder = new HubConnectionBuilder().WithUrl(serviceUri, options =>
                {
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                    if (cli.AccessTokenOption.HasValue())
                    {
                        sb.Append(" token:on");
                        options.AccessTokenProvider = () =>
                        {
                            return Task.FromResult(cli.AccessTokenOption.Value());
                        };
                    }

                    if (cli.SkipNegotiate.HasValue())
                    {
                        sb.Append(" skip negotiate:on");
                        options.SkipNegotiation = true;
                    }
                });
                hubConnectionBuilder.AddNewtonsoftJsonProtocol();
                if (cli.MessagePackOption.HasValue())
                {
                    sb.Append(" messagePack:on");
                    hubConnectionBuilder.AddMessagePackProtocol((options) =>
                    {
                    });
                }

                if (cli.DebugSignalROption.HasValue())
                {
                    sb.Append(" debug:on");
                    hubConnectionBuilder.ConfigureLogging(logging =>
                    {
                        // Log to the Console
                        logging.AddConsole();

                        // This will set ALL logging to Debug level
                        logging.SetMinimumLevel(LogLevel.Debug);
                    });
                }

                TraceSource.Verbose(sb.ToString());
                return hubConnectionBuilder.Build();
            };

            TraceSource = CreateTraceSource("SignalR.CLI");
            TraceSource.Verbose($"Started CLI using serviceEndpoint:{serviceEndpoint}");

            await OnStartedAsync();
            Console.WriteLine("Accepting key options...");
            while (true)
            {
                var key = Console.ReadKey();
                Console.WriteLine($"Option:{key.KeyChar} selected");
                if (key.KeyChar == 'q')
                {
                    this.disposeCts.Cancel();
                    await DiposeAsync();
                    break;
                }
                else if (CanProcessKey(key.KeyChar))
                {
                    try
                    {
                        await HandleKeyAsync(key.KeyChar);
                    }
                    catch (Exception error)
                    {
                        await Console.Error.WriteLineAsync($"Failed to process option:'{key.KeyChar}' error:{error}");
                    }
                }
            }

            return 0;
        }

        protected HubConnection CreateHubConnection(string serviceUri = null) => this.hubConnectionFactory(serviceUri);

        protected virtual Task OnStartedAsync()
        {
            return Task.CompletedTask;
        }

        protected abstract Task DiposeAsync();

        protected abstract Task HandleKeyAsync(char key);

        protected virtual bool CanProcessKey(char key) => true;

        private static bool IsUniversalHubUri(string serviceEndpoint)
        {
            if (string.IsNullOrEmpty(serviceEndpoint))
            {
                return true;
            }

            return serviceEndpoint.EndsWith(SignalRHubName) || serviceEndpoint.EndsWith(SignalRHubDevName);
        }

        private static TraceSource CreateTraceSource(string name)
        {
            var traceSource = new TraceSource(name);
            var consoleTraceListener = new ConsoleTraceListener();
            traceSource.Listeners.Add(consoleTraceListener);
            traceSource.Switch.Level = SourceLevels.All;
            return traceSource;
        }

        private class ConsoleTraceListener : TraceListener
        {
            public override void Write(string message)
            {
                Console.Write(message);
            }

            public override void WriteLine(string message)
            {
                Console.WriteLine(message);
            }
        }
    }
}
