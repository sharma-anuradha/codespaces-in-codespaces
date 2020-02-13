// <copyright file="RelayHubApp.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Client;
using Microsoft.VsCloudKernel.SignalService.Common;
using StreamJsonRpc;

namespace SignalService.Client.CLI
{
    internal class RelayHubApp : SignalRApp
    {
        private const string TypeTest = "test";
        private const string TypeJsonRpc = "jsonRpc";
        private const string Method1 = "method1";
        private const string Notify1 = "notify1";

        private readonly CommandOption userIdOption;
        private RelayServiceProxy relayServiceProxy;
        private string lastHubId = "test";
        private IRelayHubProxy currentRelayHubProxy;
        private JsonRpc currentJsonRpc;

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

                Console.WriteLine($"Successfully joined on service id:{this.currentRelayHubProxy.ServiceId} stamp:{this.currentRelayHubProxy.Stamp}");

                foreach (var participant in this.currentRelayHubProxy.Participants)
                {
                    Console.WriteLine(ToString(participant));
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
                    Console.WriteLine($"No relay hub joined!");
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
            else if (key == 'l')
            {
                if (this.currentRelayHubProxy == null)
                {
                    Console.WriteLine($"No relay hub joined!");
                    return;
                }

                var hubId = this.currentRelayHubProxy.Id;
                await this.currentRelayHubProxy.DisposeAsync();
                this.currentRelayHubProxy = null;
                Console.WriteLine($"leave Hub id:{hubId}");
            }
            else if (key == 'd')
            {
                if (this.currentRelayHubProxy == null)
                {
                    Console.WriteLine($"No relay hub joined!");
                    return;
                }

                var hubId = this.currentRelayHubProxy.Id;
                this.currentRelayHubProxy = null;
                await this.relayServiceProxy.DeleteHubAsync(hubId, DisposeToken);
                Console.WriteLine($"Hub id:{hubId} deleted...");
            }
            else if (key == 'h')
            {
                if (this.currentRelayHubProxy == null)
                {
                    Console.WriteLine($"Please join a hub to continue");
                }
                else
                {
                    var targetParticipant = SelectParticipant(this.currentRelayHubProxy);
                    if (targetParticipant != null)
                    {
                        var targetParticipantId = targetParticipant.Id;
                        var stream = new RelayHubStream(this.currentRelayHubProxy, targetParticipantId, TypeJsonRpc);
                        var jsonRpc = new JsonRpc(stream);
                        Func<int, string, string> method1 = (value, str) =>
                        {
                            return $"value:{value} str:{str}";
                        };
                        jsonRpc.AddLocalRpcMethod(Method1, method1);

                        Action<int> notify1 = (value) =>
                        {
                            Console.WriteLine($"notify value:{value}");
                        };

                        jsonRpc.AddLocalRpcMethod(Notify1, notify1);

                        jsonRpc.StartListening();
                        jsonRpc.Disconnected += (s, e) =>
                        {
                            Console.WriteLine($"Rpc channel disconnected for participant:{targetParticipantId}");
                            this.currentJsonRpc = null;
                        };
                        this.currentJsonRpc = jsonRpc;
                        Console.WriteLine($"Listening json rpc channel for participant:{targetParticipantId}");
                    }
                }
            }
            else if (key == 'm')
            {
                if (this.currentJsonRpc == null)
                {
                    Console.WriteLine($"No json rpc stream defined");
                }
                else
                {
                    var result = await this.currentJsonRpc.InvokeAsync<string>(Method1, 100, "Hello");
                    Console.WriteLine($"Returned result:{result}");
                }
            }
            else if (key == 'n')
            {
                if (this.currentJsonRpc == null)
                {
                    Console.WriteLine($"No json rpc stream defined");
                }
                else
                {
                    await this.currentJsonRpc.NotifyAsync(Notify1, 256);
                }
            }
        }

        protected override void OnHubCreated()
        {
            this.relayServiceProxy = HubProxy.CreateHubProxy<RelayServiceProxy>(HubClient, TraceSource, true);
        }

        private static IRelayHubParticipant SelectParticipant(IRelayHubProxy relayHubProxy)
        {
            var targetParticipants = relayHubProxy.Participants.Where(p => !p.IsSelf).ToArray();
            int index = 1;
            foreach (var participant in targetParticipants)
            {
                Console.WriteLine($"({index}) =>{ToString(participant)}");
            }

            index = 1;
            Utils.ReadIntValue("Select participant:", ref index);
            if (index <= targetParticipants.Length)
            {
                return targetParticipants[index - 1];
            }

            Console.WriteLine($"Invalid participant index");
            return null;
        }

        private static string ToString(IRelayHubParticipant relayHubParticipant)
        {
            return $"Participant id:{relayHubParticipant.Id} properties:{relayHubParticipant.Properties.ConvertToString(null)} isSelf:{relayHubParticipant.IsSelf}";
        }
    }
}
