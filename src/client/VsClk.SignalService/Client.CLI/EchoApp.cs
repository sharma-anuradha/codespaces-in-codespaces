// <copyright file="EchoApp.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService.Client;
using Newtonsoft.Json.Linq;

namespace SignalService.Client.CLI
{
    internal class EchoApp : SignalRAppBase
    {
        private int echoSecs;
        private int nFailures;
        private int nTotal;

        public EchoApp(int echoSecs)
        {
            this.echoSecs = echoSecs;
        }

        protected override Task HandleKeyAsync(char key) => Task.CompletedTask;

        protected override Task DiposeAsync()
        {
            return Task.CompletedTask;
        }

        protected override async Task OnStartedAsync()
        {
            var hubConnection = CreateHubConnection();
            while (true)
            {
                try
                {
                    ++this.nTotal;
                    var start = Stopwatch.StartNew();
                    await hubConnection.StartAsync(DisposeToken);
                    var result = await hubConnection.InvokeAsync<JObject>("Echo", "Hello from CLI", DisposeToken);
                    Console.WriteLine($"Succesfully received echo -> result:{result} time(ms):{start.ElapsedMilliseconds} total:{this.nTotal} failures:{this.nFailures}");
                }
                catch (Exception err)
                {
                    ++this.nFailures;
                    Console.WriteLine($"Failed to echo -> err:{err} failures:{this.nFailures}");
                }
                finally
                {
                    if (hubConnection.State == HubConnectionState.Connected)
                    {
                        await hubConnection.StopAsync();
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(echoSecs), DisposeToken);
            }
        }
    }
}
