// <copyright file="EchoApp.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService.Client;
using Newtonsoft.Json.Linq;

namespace SignalService.Client.CLI
{
    internal class EchoApp : SignalRApp
    {
        private int echoSecs;

        public EchoApp(int echoSecs)
        {
            this.echoSecs = echoSecs;
        }

        protected override string HubName => "healthhub";

        protected override Task HandleKeyAsync(char key) => Task.CompletedTask;

        protected override void OnHubCreated()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (HubClient.IsConnected)
                    {
                        try
                        {
                            var result = await HubClient.Connection.InvokeAsync<JObject>("Echo", "Hello from CLI", DisposeToken);
                            Console.WriteLine($"Succesfully received echo -> result:{result.ToString()}");
                        }
                        catch (Exception err)
                        {
                            Console.WriteLine($"Failed to echo -> err:{err}");
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(echoSecs), DisposeToken);
                }
            }).Forget();
        }
    }
}
