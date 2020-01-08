// <copyright file="SignalRAppBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService.Client;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace SignalService.Client.CLI
{
    /// <summary>
    /// Base signalR app class.
    /// </summary>
    internal abstract class SignalRAppBase
    {
        private const string DefaultServiceEndpointBase = "https://localhost:5001/";

        private CancellationTokenSource disposeCts = new CancellationTokenSource();

        private Func<HubConnection> hubConnectionFactory;

        protected CancellationToken DisposeToken => this.disposeCts.Token;

        protected virtual string HubName => "signalrhub";

        protected TraceSource TraceSource { get; private set; }

        public async Task<int> RunAsync(Program cli)
        {
            string serviceEndpoint = cli.ServiceEndpointOption.Value();
            if (string.IsNullOrEmpty(serviceEndpoint))
            {
                serviceEndpoint = DefaultServiceEndpointBase + HubName;
            }

            this.hubConnectionFactory = () =>
            {
                IHubConnectionBuilder hubConnectionBuilder;
                if (cli.AccessTokenOption.HasValue())
                {
                    hubConnectionBuilder = HubConnectionHelpers.FromUrlAndAccessToken(serviceEndpoint, cli.AccessTokenOption.Value());
                }
                else
                {
                    hubConnectionBuilder = HubConnectionHelpers.FromUrl(serviceEndpoint);
                }

                if (cli.DebugSignalROption.HasValue())
                {
                    hubConnectionBuilder.ConfigureLogging(logging =>
                    {
                        // Log to the Console
                        logging.AddConsole();

                        // This will set ALL logging to Debug level
                        logging.SetMinimumLevel(LogLevel.Debug);
                    });
                }

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

        protected HubConnection CreateHubConnection() => this.hubConnectionFactory();

        protected virtual Task OnStartedAsync()
        {
            return Task.CompletedTask;
        }

        protected abstract Task DiposeAsync();

        protected abstract Task HandleKeyAsync(char key);

        protected virtual bool CanProcessKey(char key) => true;

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
