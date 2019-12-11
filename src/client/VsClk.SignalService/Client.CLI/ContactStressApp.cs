using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Client;
using Microsoft.VsCloudKernel.SignalService.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SignalService.Client.CLI
{
    internal class ContactStressApp : SignalRAppBase
    {
        private const string TypeTestMessage = "typeTest";
        private static Random random = new Random();

        private int batchRequests = 1000;
        private int numberOfContactsPerBatch = 400;
        private int numberOfEmailRequests = 100;

        private int sendBatchDelayMillsecs = 500;
        private int receivedMessages = 0;
        private int numberOfTotalMessage = 0;
        private TimeSpan totalCloudTime;
        private object totalCloudTimeLock = new object();

        private readonly CommandOption contactsFilePathOption;
        private int currentBatchId;
        private Dictionary<int, Dictionary<string, PresenceEndpoint>> allEndpoints = new Dictionary<int, Dictionary<string, PresenceEndpoint>>();
        private CancellationTokenSource sendCts;

        public ContactStressApp(CommandOption contactsFilePathOption)
        {
            this.contactsFilePathOption = contactsFilePathOption;
        }

        private string ContactsFilePath => this.contactsFilePathOption.HasValue() ? contactsFilePathOption.Value() : null;

        private IEnumerable<PresenceEndpoint> AllEndpoints => this.allEndpoints.SelectMany(i => i.Value.Values);
 
        private int NumberOfConnectedEndpoints => AllEndpoints.Where(i => i.HubClient.State == HubConnectionState.Connected).Count();

        protected override Task DiposeAsync()
        {
            return Task.CompletedTask;
        }

        protected override async Task HandleKeyAsync(char key)
        {
            if (key == 'i')
            {
                TraceSource.Verbose($"Total:{AllEndpoints.Count()} connected:{NumberOfConnectedEndpoints}");
            }
            else if (key == 's')
            {
                if (ContactsFilePath == null)
                {
                    TraceSource.Verbose($"No contacts file was defined");
                    return;
                }

                var contactIds = this.allEndpoints.SelectMany(i => i.Value.Keys).Distinct().ToArray();
                SaveContactIdsToFile(ContactsFilePath, contactIds);
                TraceSource.Verbose($"Saved contacts to path:{ContactsFilePath} total:{contactIds.Length}");
            }
            else if (key == 'l')
            {
                if (ContactsFilePath == null)
                {
                    TraceSource.Verbose($"No contacts file was defined");
                    return;
                }

                var contactIds = LoadContactIdsFromFile(ContactsFilePath);
                TraceSource.Verbose($"Load contacts from path:{ContactsFilePath} total:{contactIds.Length}");
                await CreateEndpointsAsync(contactIds);
            }
            else if (key == '+')
            {
                ReadIntValue("Enter batch count:", ref this.batchRequests);
                await CreateEndpointsAsync(Enumerable.Range(0, this.batchRequests).Select(i => Guid.NewGuid().ToString()));
            }
            else if (key == 'x')
            {
                ReadIntValue("Enter number of contacts per batch:", ref this.numberOfContactsPerBatch);
                ReadIntValue("Enter batch delay in millisecs:", ref this.sendBatchDelayMillsecs);
                ReadIntValue("Enter number of total messages:", ref this.numberOfTotalMessage);
                bool enforceCrossService = false;
                ReadBoolValue("Enforce cross region:", ref enforceCrossService);
                this.receivedMessages = 0;
                this.totalCloudTime = default;

                var task = SendAllAsync(enforceCrossService);
            }
            else if (key == 'e')
            {
                this.sendCts?.Cancel();
            }
            else if (key == 'r')
            {
                ReadIntValue("Enter number of emails to request:", ref this.numberOfEmailRequests);
                var contactId = Guid.NewGuid().ToString();
                TraceSource.Verbose($"Creating endpoint:{contactId}...");
                var endpoint = await PresenceEndpoint.CreateAsync(contactId, CreateHubConnection(), TraceSource, DisposeToken);
                var start = Stopwatch.StartNew();
                await endpoint.Proxy.RequestSubcriptionsAsync(
                    Enumerable.Repeat(0, this.numberOfEmailRequests).Select(i => new Dictionary<string, object>() { { ContactProperties.Email, CreateEmail(10) } }).ToArray(),
                    new string[] { "status" },
                    true,
                    DisposeToken);
                TraceSource.Verbose($"Finished time->{start.ElapsedMilliseconds}");
            }
        }

        private void OnMessageReceived(object sender, ReceiveMessageEventArgs e)
        {
            if (e.Type == TypeTestMessage && this.sendCts?.IsCancellationRequested == false)
            {
                var messageObject = ((JObject)e.Body).ToObject<MessageBody>();
                var messageDeliveryTime = DateTime.Now - messageObject.SentTimestamp;
                lock (this.totalCloudTimeLock)
                {
                    this.totalCloudTime = totalCloudTime.Add(messageDeliveryTime);
                }
            }

            if (Interlocked.Increment(ref this.receivedMessages) == this.numberOfTotalMessage)
            {
                this.sendCts?.Cancel();
                Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    TraceSource.Info($"Finish send stress received messages:{this.receivedMessages} totalTime:{this.totalCloudTime.TotalSeconds}");
                });
            }
        }

        private static void ReadIntValue(string message, ref int value)
        {
            Console.Write($"{message}({value}):");
            var line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                value = int.Parse(line);
            }
        }

        private static void ReadBoolValue(string message, ref bool value)
        {
            Console.Write($"{message}({value}):");
            var line = Console.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                value = bool.Parse(line);
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
            Dictionary<string, PresenceEndpoint> endpoints,
            int numOfContacts,
            TraceSource traceSource,
            bool enforceCrossService,
            CancellationToken cancellationToken)
        {
            var rand = new Random();
            var items = GetRandomItems(endpoints).Where(i => i.Value.HubClient.State == HubConnectionState.Connected).Take(numOfContacts).ToList();

            var tasks = new List<Task>();
            foreach (var item in items)
            {
                // Next block will attempt to find a target contact id that could be
                // enforced to be on another service
                (string, string) targetContact = default;
                for (int index = 0;index < items.Count; ++index)
                {
                    var target = items[rand.Next(items.Count)];
                    if (!enforceCrossService || target.Value.ServiceId != item.Value.ServiceId)
                    {
                        targetContact = (target.Key, target.Value.ServiceId);
                        break;
                    }
                }

                if (targetContact == default)
                {
                    continue;
                }

                traceSource.Verbose($"Sending message from:({item.Key}, {item.Value.ServiceId}) to:({targetContact.Item1},{targetContact.Item2})");

                tasks.Add(item.Value.Proxy.SendMessageAsync(
                    new ContactReference(targetContact.Item1, null),
                    TypeTestMessage,
                    new MessageBody()
                    {
                        SentTimestamp = DateTime.Now,
                        Text = $"Message from contact id:{item.Key}",
                    },
                    cancellationToken));
            }

            await WaitAllAsync(tasks, traceSource, cancellationToken);
        }

        private static IEnumerable<KeyValuePair<string, PresenceEndpoint>> GetRandomItems(Dictionary<string, PresenceEndpoint> connections)
        {
            var rand = new Random();
            var allItems = Enumerable.ToList(connections);
            int size = connections.Count;
            while (true)
            {
                yield return allItems[rand.Next(size)];
            }
        }

        private static string[] LoadContactIdsFromFile(string jsonFilePath)
        {
            var json = File.ReadAllText(jsonFilePath);
            return JsonConvert.DeserializeObject<string[]>(json);
        }

        private static void SaveContactIdsToFile(string jsonFilePath, string[] contactIds)
        {
            File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(contactIds));
        }

        private async Task SendAllAsync(bool enforceCrossService)
        {
            this.sendCts = new CancellationTokenSource();
            try
            {
                while (!this.sendCts.IsCancellationRequested)
                {
                    foreach (var connections in allEndpoints.Values)
                    {
                        await SendMessagesAsync(connections, this.numberOfContactsPerBatch, TraceSource, enforceCrossService, this.sendCts.Token);
                        await Task.Delay(this.sendBatchDelayMillsecs, this.sendCts.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }

            TraceSource.Verbose($"Send test finished...");
        }

        private async Task CreateEndpointsAsync(IEnumerable<string> contactIds)
        {
            var tasks = new List<Task<PresenceEndpoint>>();

            // create a new batch of connections
            foreach (var contactId in contactIds)
            {
                TraceSource.Verbose($"Create presence endpoint for contact:{contactId}");
                tasks.Add(PresenceEndpoint.CreateAsync(
                    contactId,
                    CreateHubConnection(),
                    TraceSource,
                    DisposeToken));
            }

            var batchConnections = new Dictionary<string, PresenceEndpoint>();
            await WaitAllAsync(
                tasks,
                (result) =>
                {
                    result.Proxy.MessageReceived += OnMessageReceived;
                    batchConnections[result.ContactId] = result;
                },
                TraceSource,
                CancellationToken.None);

            ++this.currentBatchId;
            this.allEndpoints[this.currentBatchId] = batchConnections;

            TraceSource.Verbose($"Completed total connections:{NumberOfConnectedEndpoints} for batch:{this.currentBatchId}");
        }

        private static string CreateEmail(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return $"{new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray())}@microsoft.com";
        }

        private struct MessageBody
        {
            public string Text { get; set; }

            public DateTime SentTimestamp { get; set; }
        }

        private class PresenceEndpoint
        {
            private readonly TraceSource traceSource;

            internal PresenceEndpoint(string contactId, HubClient hubClient, TraceSource traceSource)
            {
                ContactId = contactId;
                HubClient = hubClient;
                this.traceSource = traceSource;
                Proxy = HubProxy.CreateHubProxy<ContactServiceProxy>(hubClient.Connection, traceSource, true);
                HubClient.ConnectionStateChanged += OnConnectionStateChangedAsync;
            }

            public async Task DisposeAsync()
            {
                await Proxy.UnregisterSelfContactAsync(CancellationToken.None);
                await HubClient.StopAsync(CancellationToken.None);
            }

            public HubClient HubClient { get; private set; }

            public ContactServiceProxy Proxy { get; }

            public string ContactId { get; }

            public string ServiceId { get; private set; }

            public string Stamp { get; private set; }

            public static async Task<PresenceEndpoint> CreateAsync(string contactId, HubConnection hubConnection, TraceSource traceSource, CancellationToken cancellationToken)
            {
                traceSource.Verbose($"Creating endpoint for contactId:{contactId}");

                // try once
                await hubConnection.StartAsync(cancellationToken);

                var hubClient = new HubClient(hubConnection, traceSource);

                // next call will make sure the HubClinet is in running state
                await hubClient.StartAsync(CancellationToken.None);

                var endpoint = new PresenceEndpoint(contactId, hubClient, traceSource);
                await endpoint.RegisterAsync(cancellationToken);
                return endpoint;
            }

            private async Task OnConnectionStateChangedAsync(object sender, EventArgs args)
            {
                if (HubClient.State == HubConnectionState.Connected)
                {
                    await RegisterAsync(CancellationToken.None);
                }
            }

            private async Task RegisterAsync(CancellationToken cancellationToken)
            {
                var publishedProperties = new Dictionary<string, object>()
                {
                    { "status", "available" },
                };

                var factoryProperties = await Proxy.RegisterSelfContactAsync(ContactId, publishedProperties, cancellationToken);
                ServiceId = factoryProperties[ContactProperties.ServiceId].ToString();
                Stamp = factoryProperties[ContactProperties.Stamp].ToString();
                this.traceSource.Verbose($"registration completed for contactId:{ContactId} on serviceId:{ServiceId} stamp:{Stamp}");
            }
        }
    }
}
