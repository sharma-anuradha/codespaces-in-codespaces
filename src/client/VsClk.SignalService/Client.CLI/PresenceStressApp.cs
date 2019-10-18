using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Client;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace SignalService.Client.CLI
{
    internal class PresenceStressApp : SignalRAppBase
    {
        private int batchRequests = 1000;
        private int currentBatchId;
        private Dictionary<int, Dictionary<string, (HubClient, PresenceServiceProxy)>> allConnections = new Dictionary<int, Dictionary<string, (HubClient, PresenceServiceProxy)>>();
        private CancellationTokenSource sendCts;

        private int NumberOfConnections => this.allConnections.Sum(b => b.Value.Values.Count(i => i.Item1.State == HubConnectionState.Connected));

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

                var tasks = new List<Task<(string, HubClient, PresenceServiceProxy)>>();

                // create a new batch of connections
                for (int i = 0; i < this.batchRequests; ++i)
                {
                    var contactId = Guid.NewGuid().ToString();
                    TraceSource.Verbose($"Create presence endpoint for contact:{contactId}");
                    tasks.Add(CreatePresenceEndpointAsync(contactId));
                }

                var batchConnections = new Dictionary<string, (HubClient, PresenceServiceProxy)>();
                await WaitAllAsync(
                    tasks,
                    (result) =>
                    {
                        batchConnections[result.Item1] = (result.Item2, result.Item3);
                    },
                    TraceSource,
                    CancellationToken.None);

                ++this.currentBatchId;
                this.allConnections[this.currentBatchId] = batchConnections;

                TraceSource.Verbose($"Completed batch request total connections:{NumberOfConnections}");
            }
            else if (key == 's')
            {
                var task = SendAllAsync();
            }
            else if (key == 'e')
            {
                this.sendCts?.Cancel();
            }
        }

        private static async Task WaitAllAsync<TResult>(
            List<Task<TResult>> tasks,
            Action<TResult> onResult,
            TraceSource traceSource,
            CancellationToken cancellationToken)
        {
            while (tasks.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var task = await Task.WhenAny(tasks);
                    tasks.Remove(task);
                    var result = await task;
                    onResult(result);
                }
                catch (Exception error)
                {
                    traceSource.Error($"Failed to complete task with error:{error.Message}");
                }
            }
        }

        private static async Task WaitAllAsync(
            List<Task> tasks,
            TraceSource traceSource,
            CancellationToken cancellationToken)
        {
            while (tasks.Count > 0 && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var task = await Task.WhenAny(tasks);
                    tasks.Remove(task);
                    await task;
                }
                catch (Exception error)
                {
                    traceSource.Error($"Failed to complete task with error:{error.Message}");
                }
            }
        }

        private static async Task SendMessagesAsync(
            Dictionary<string, (HubClient, PresenceServiceProxy)> connections,
            int numOfContacts,
            TraceSource traceSource,
            CancellationToken cancellationToken)
        {
            var rand = new Random();
            var items = GetRandomItems(connections).Where(i => i.Value.Item1.State == HubConnectionState.Connected).Take(numOfContacts).ToList();

            var tasks = new List<Task>();
            foreach (var item in items)
            {
                var target = items[rand.Next(items.Count)];
                traceSource.Verbose($"Sending message from:{item.Key} to:{target.Key}");

                tasks.Add(item.Value.Item2.SendMessageAsync(
                    new ContactReference(target.Key, null),
                    "typeTest",
                    $"Message from id:{item.Key}",
                    cancellationToken));
            }

            await WaitAllAsync(tasks, traceSource, cancellationToken);
        }

        private static IEnumerable<KeyValuePair<string, (HubClient, PresenceServiceProxy)>> GetRandomItems(Dictionary<string, (HubClient, PresenceServiceProxy)> connections)
        {
            var rand = new Random();
            var allItems = Enumerable.ToList(connections);
            int size = connections.Count;
            while (true)
            {
                yield return allItems[rand.Next(size)];
            }
        }

        private async Task SendAllAsync()
        {
            this.sendCts = new CancellationTokenSource();
            while (!this.sendCts.IsCancellationRequested)
            {
                foreach (var connections in allConnections.Values)
                {
                    await SendMessagesAsync(connections, 400, TraceSource, this.sendCts.Token);
                    await Task.Delay(200, this.sendCts.Token);
                }
            }
        }

        private async Task<(string, HubClient, PresenceServiceProxy)> CreatePresenceEndpointAsync(string contactId)
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
            return (contactId, hubClient, presenceServiceProxy);
        }
    }
}
