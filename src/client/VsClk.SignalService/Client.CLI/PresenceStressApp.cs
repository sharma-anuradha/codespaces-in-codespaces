using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsCloudKernel.SignalService.Client;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace SignalService.Client.CLI
{
    internal class PresenceStressApp : SignalRAppBase
    {
        private int batchRequests = 1000;
        private int currentBatchId;
        private Dictionary<int, Dictionary<string, HubClient>> totalConnections = new Dictionary<int, Dictionary<string, HubClient>>();

        protected override Task DiposeAsync()
        {
            return Task.CompletedTask;
        }

        protected override async Task HandleKeyAsync(char key)
        {
            if (key == '+')
            {
                Console.Write($"Enter batch count:({this.batchRequests}):");
                var batchCountLine = Console.ReadLine();
                if (!string.IsNullOrEmpty(batchCountLine))
                {
                    this.batchRequests = int.Parse(batchCountLine);
                }

                var tasks = new List<Task<(string, HubClient)>>();

                // create a new batch of connections
                for (int i = 0; i < this.batchRequests; ++i)
                {
                    var contactId = Guid.NewGuid().ToString();
                    TraceSource.Verbose($"Create presence endpoint for contact:{contactId}");
                    tasks.Add(CreatePresenceEndpointAsync(contactId));
                }

                var batchConnections = new Dictionary<string, HubClient>();
                while (tasks.Count > 0)
                {
                    try
                    {
                        var task = await Task.WhenAny(tasks);
                        tasks.Remove(task);
                        var result = await task;
                        batchConnections[result.Item1] = result.Item2;
                    }
                    catch (Exception error)
                    {
                        TraceSource.Error($"Failed to complete task with error:{error.Message}");
                    }
                }

                ++this.currentBatchId;
                this.totalConnections[this.currentBatchId] = batchConnections;

                TraceSource.Verbose($"Completed batch request total connections:{this.totalConnections.Sum(b => b.Value.Count)}");
            }
        }

        private async Task<(string, HubClient)> CreatePresenceEndpointAsync(string contactId)
        {
            var hubConnection = CreateHubConnection();
            TraceSource.Verbose($"hubConnection connected for contactId:{contactId}");

            // try once
            await hubConnection.StartAsync(DisposeToken);

            var hubClient = new HubClient(hubConnection, TraceSource);
            var presenceServiceProxy = HubProxy.CreateHubProxy<PresenceServiceProxy>(hubClient.Connection, TraceSource, true);

            var publishedProperties = new Dictionary<string, object>()
            {
                { "status", "available" },
            };

            await presenceServiceProxy.RegisterSelfContactAsync(contactId, publishedProperties, DisposeToken);
            TraceSource.Verbose($"registration completed for contactId:{contactId}");
            return (contactId, hubClient);
        }
    }
}
