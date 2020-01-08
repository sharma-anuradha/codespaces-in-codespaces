// <copyright file="RelayHubApp.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Client;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace SignalService.Client.CLI
{
    internal class RelayHubApp : SignalRApp
    {
        private const string TypeTest = "test";

        private readonly CommandOption userIdOption;
        private RelayServiceProxy relayServiceProxy;
        private string lastHubId;
        private IRelayHubProxy currentRelayHubProxy;

        public RelayHubApp(CommandOption userIdOption)
        {
            this.userIdOption = userIdOption;
        }

        protected override async Task HandleKeyAsync(char key)
        {
            if (key == 'c')
            {
                Console.Write($"Enter hub id:('null'):");
                var hubId = Console.ReadLine();
                if (string.IsNullOrEmpty(hubId))
                {
                    hubId = null;
                }

                this.lastHubId = await this.relayServiceProxy.CreateHubAsync(hubId, DisposeToken);
                Console.WriteLine($"Created hub with id:{this.lastHubId}");
            }
            else if (key == 'j')
            {
                Console.Write($"Enter hub id:({this.lastHubId}):");
                var hubId = Console.ReadLine();
                if (string.IsNullOrEmpty(hubId))
                {
                    hubId = this.lastHubId;
                }

                this.currentRelayHubProxy = await this.relayServiceProxy.JoinHubAsync(
                    hubId,
                    new Dictionary<string, object>()
                    {
                            { "userId", this.userIdOption.HasValue() ? this.userIdOption.Value() : "none" },
                    },
                    true,
                    DisposeToken);

                Console.WriteLine($"Successfully joined...");

                foreach (var participant in this.currentRelayHubProxy.Participants)
                {
                    Console.WriteLine($"Participant id:{participant.Id} properties:{participant.Properties.ConvertToString(null)} isSelf:{participant.IsSelf}");
                }

                this.currentRelayHubProxy.ReceiveData += (s, e) =>
                {
                    if (e.Type == TypeTest)
                    {
                        var message = Encoding.UTF8.GetString(e.Data);
                        Console.WriteLine($"Received message:{message}");
                    }
                };
            }
            else if (key == 's')
            {
                if (this.currentRelayHubProxy == null)
                {
                    Console.WriteLine($"No realy hub joined!");
                    return;
                }

                Console.Write($"Enter message:('Hi'):");
                var message = Console.ReadLine();
                if (string.IsNullOrEmpty(message))
                {
                    message = "Hi";
                }

                Console.Write($"Enter participant id:('*'):");
                var participantId = Console.ReadLine();

                await this.currentRelayHubProxy.SendDataAsync(SendOption.None, string.IsNullOrEmpty(participantId) ? null : new string[] { participantId }, TypeTest, Encoding.UTF8.GetBytes(message), DisposeToken);
            }
        }

        protected override void OnHubCreated()
        {
            this.relayServiceProxy = HubProxy.CreateHubProxy<RelayServiceProxy>(HubClient.Connection, TraceSource, true);
        }
    }
}
