using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Client;
using Microsoft.VsCloudKernel.SignalService.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LivesharePresenceClientTest
{
    internal class Program : CommandLineApplication
    {
        private CommandOption serviceEndpointOption;
        private CommandOption accessTokenOption;
        private CommandOption debugSignalROption;

        public static int Main(string[] args)
        {
            var cli = new Program();

            cli.serviceEndpointOption = cli.Option(
                "-s | --service",
                "Override the web service endpoint URI",
                CommandOptionType.SingleValue);

            cli.accessTokenOption = cli.Option(
                "--accessToken",
                "Access token for authentication",
                CommandOptionType.SingleValue);

            cli.debugSignalROption = cli.Option(
                "--debugSignalR",
                "If SignalR tracing is enabled",
                CommandOptionType.NoValue);

            cli.Command("run-echo", app =>
            {
                var echoOption = cli.Option(
                "--echo",
                "Perfom echo every (n) secs on the endpoint",
                CommandOptionType.SingleValue);

                app.Description = "Run echo hub mode";

                app.OnExecute(() => new EchoApp(int.Parse(echoOption.Value())).RunAsync(cli));
            });

            cli.Command("run-presence", app =>
            {
                var contactIdOption = cli.Option(
                    "--id",
                    "Contact id to use",
                    CommandOptionType.SingleValue);

                var emailOption = cli.Option(
                    "--email",
                    "Email to use",
                    CommandOptionType.SingleValue);

                var nameOption = cli.Option(
                    "--name",
                    "Name to use",
                    CommandOptionType.SingleValue);

                var usePresenceHubNameOption = cli.Option(
                    "--usePresenceHubName",
                    "If we want to connect to the presence hub endpoint",
                    CommandOptionType.NoValue);

                app.OnExecute(() => new PresenceApp(contactIdOption, emailOption, nameOption, usePresenceHubNameOption).RunAsync(cli));
            });

            cli.Command("run-relay", app =>
            {
                var userIdOption = cli.Option(
                    "--userId",
                    "User id to use when joining",
                    CommandOptionType.SingleValue);

                app.OnExecute(() => new RelayApp(userIdOption).RunAsync(cli));
            });

            try
            {
                return cli.Execute(args);
            }
            catch (CommandParsingException cpex)
            {
                Console.Error.WriteLine(cpex.Message);
                cli.ShowHelp();
                return 1;
            }
            catch (Exception ex)
            {
                if (ex is AggregateException aex && aex.InnerException != null)
                {
                    ex = aex.InnerException;
                }

                Console.Error.WriteLine(ex);
                return 2;
            }
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

        private abstract class SignalRApp
        {
            private const string DefaultServiceEndpointBase = "https://localhost:5001/";

            private CancellationTokenSource disposeCts = new CancellationTokenSource();

            protected CancellationToken DisposeToken => this.disposeCts.Token;

            protected virtual string HubName => "signalrhub";

            public async Task<int> RunAsync(Program cli)
            {
                string serviceEndpoint = cli.serviceEndpointOption.Value();
                if (string.IsNullOrEmpty(serviceEndpoint))
                {
                    serviceEndpoint = DefaultServiceEndpointBase + HubName;
                }

                IHubConnectionBuilder hubConnectionBuilder;
                if (cli.accessTokenOption.HasValue())
                {
                    hubConnectionBuilder = HubConnectionHelpers.FromUrlAndAccessToken(serviceEndpoint, cli.accessTokenOption.Value());
                }
                else
                {
                    hubConnectionBuilder = HubConnectionHelpers.FromUrl(serviceEndpoint);
                }

                if (cli.debugSignalROption.HasValue())
                {
                    hubConnectionBuilder.ConfigureLogging(logging =>
                    {
                        // Log to the Console
                        logging.AddConsole();

                        // This will set ALL logging to Debug level
                        logging.SetMinimumLevel(LogLevel.Debug);
                    });
                }

                var traceSource = CreateTraceSource("SignalR.CLI");
                traceSource.Verbose($"Started CLI using serviceEndpoint:{serviceEndpoint}");

                var hubClient = new HubClient(hubConnectionBuilder.Build(), traceSource);
                hubClient.StartAsync(DisposeToken).Forget();

                OnHubCreated(hubClient, traceSource);

                Console.WriteLine("Accepting key options...");
                while (true)
                {
                    var key = Console.ReadKey();
                    Console.WriteLine($"Option:{key.KeyChar} selected");
                    if (key.KeyChar == 'q')
                    {
                        this.disposeCts.Cancel();
                        await hubClient.StopAsync(CancellationToken.None);

                        break;
                    }
                    else if (!hubClient.IsConnected)
                    {
                        Console.WriteLine("Waiting for Connection...");
                    }
                    else
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

            protected abstract Task HandleKeyAsync(char key);

            protected abstract void OnHubCreated(HubClient hubClient, TraceSource traceSource);
        }

        private class PresenceApp : SignalRApp
        {
            private string contactId;
            private string email;
            private PresenceServiceProxy presenceServiceProxy;
            private CommandOption nameOption;
            private CommandOption usePresenceHubNameOption;

            private string[] subscribeProperties = new string[] { "status", "email" };
            private string publishProperty = "status";

            public PresenceApp(
                CommandOption contactIdOption,
                CommandOption emailOption,
                CommandOption nameOption,
                CommandOption usePresenceHubNameOption)
            {
                this.contactId = contactIdOption.HasValue() ? contactIdOption.Value() : null;
                this.email = emailOption.HasValue() ? emailOption.Value() : null;
                this.nameOption = nameOption;
                this.usePresenceHubNameOption = usePresenceHubNameOption;
            }

            protected override string HubName => this.usePresenceHubNameOption.HasValue() ? "presencehub" : base.HubName;

            protected override void OnHubCreated(HubClient hubClient, TraceSource traceSource)
            {
                this.presenceServiceProxy = HubProxy.CreateHubProxy<PresenceServiceProxy>(hubClient.Connection, traceSource, !this.usePresenceHubNameOption.HasValue());

                this.presenceServiceProxy.UpdateProperties += (s, e) =>
                {
                    Console.WriteLine($"->UpdateProperties from:{e.Contact} props:{e.Properties.ConvertToString(null)}");
                };

                this.presenceServiceProxy.MessageReceived += (s, e) =>
                {
                    Console.WriteLine($"->MessageReceived: from:{e.FromContact} type:{e.Type} body:{e.Body}");
                };

                this.presenceServiceProxy.ConnectionChanged += (s, e) =>
                {
                    Console.WriteLine($"->ConnectionChanged: from:{e.Contact} changeType:{e.ChangeType}");
                };

                hubClient.ConnectionStateChanged += async (s, e) =>
                {
                    if (hubClient.State == HubConnectionState.Connected)
                    {
                        var publishedProperties = new Dictionary<string, object>()
                        {
                            { "status", "available" },
                        };

                        if (!string.IsNullOrEmpty(email))
                        {
                            publishedProperties["email"] = email;
                        }

                        if (this.nameOption.HasValue())
                        {
                            publishedProperties.Add("name", this.nameOption.Value());
                        }

                        var registerInfo = await this.presenceServiceProxy.RegisterSelfContactAsync(contactId, publishedProperties, CancellationToken.None);
                        Console.WriteLine($"register info->{registerInfo.ConvertToString(null)}");
                    }
                };
            }

            protected override async Task HandleKeyAsync(char key)
            {
                if (key == 'b')
                {
                    Console.WriteLine("Changing status to 'busy'");
                    var updateValues = new Dictionary<string, object>()
                        {
                            {"status", "busy" },
                        };

                    await this.presenceServiceProxy.PublishPropertiesAsync(updateValues, CancellationToken.None);
                }
                else if (key == 'p')
                {
                    Console.WriteLine();
                    Console.Write($"Enter property name({publishProperty}):");
                    var line = Console.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        publishProperty = line;
                    }

                    Console.Write($"Enter property value:");
                    line = Console.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        try
                        {
                            var jTokenValue = JToken.Parse(line);
                            var updateValues = new Dictionary<string, object>()
                        {
                            { publishProperty, jTokenValue },
                        };

                            await this.presenceServiceProxy.PublishPropertiesAsync(updateValues, CancellationToken.None);
                        }
                        catch (JsonException jsonExcp)
                        {
                            Console.Write($"Failed to parse value err:{jsonExcp.Message}");
                        }
                    }
                }
                else if (key == 'a')
                {
                    Console.WriteLine("Changing status to 'available'");
                    var updateValues = new Dictionary<string, object>()
                        {
                            {"status", "available" },
                        };

                    await this.presenceServiceProxy.PublishPropertiesAsync(updateValues, CancellationToken.None);
                }
                else if (key == 'c')
                {
                    Console.WriteLine();
                    Console.Write("Enter contact id:");
                    var selfContactId = Console.ReadLine();

                    var selfConnections = await this.presenceServiceProxy.GetSelfConnectionsAsync(selfContactId, CancellationToken.None);
                    if (selfConnections.Count == 0)
                    {
                        Console.WriteLine($"No self connections available");
                    }
                    else
                    {
                        foreach (var kvp in selfConnections)
                        {
                            Console.WriteLine($"connection id:{kvp.Key} => {JsonConvert.SerializeObject(kvp.Value, Formatting.None)}");
                        }
                    }
                }
                else if (key == 's')
                {
                    Console.WriteLine();
                    Console.Write("Enter subscription contact id:");
                    var subscribeContactId = Console.ReadLine();
                    Console.WriteLine();
                    Console.Write($"Enter properties ({string.Join(',', subscribeProperties)}):");
                    var line = Console.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        subscribeProperties = line.Split(',');
                    }

                    var properties = await this.presenceServiceProxy.AddSubcriptionsAsync(
                        new ContactReference[] { new ContactReference(subscribeContactId, null) },
                        subscribeProperties,
                        CancellationToken.None);
                    Console.WriteLine($"Add subscription for id:{subscribeContactId} properties:{string.Join(',', subscribeProperties)} result:{properties[subscribeContactId].ConvertToString(null)}");
                }
                else if (key == 'u')
                {
                    Console.WriteLine();
                    Console.Write("Enter subscription contact id:");
                    var targetContactId = Console.ReadLine();

                    await presenceServiceProxy.RemoveSubscriptionAsync(
                        new ContactReference[] { new ContactReference(targetContactId, null) },
                        CancellationToken.None);
                }
                else if (key == 'm')
                {
                    Console.WriteLine();
                    Console.Write("Enter email to match:");
                    var emailMatch = Console.ReadLine();

                    var matchingContacts = await this.presenceServiceProxy.MatchContactsAsync(new Dictionary<string, object>[] { new Dictionary<string, object>() { { "email", emailMatch } } }, CancellationToken.None);
                    if (matchingContacts[0].Count == 0)
                    {
                        Console.WriteLine("No match found");
                    }
                    else
                    {
                        foreach (var matchKvp in matchingContacts[0])
                        {
                            Console.WriteLine($"Id:{matchKvp.Key} properties:{matchKvp.Value.ConvertToString(null)}");
                        }
                    }
                }
                else if (key == 'x')
                {
                    Console.WriteLine();
                    Console.Write("Enter target contact id:");
                    var targetContactId = Console.ReadLine();

                    await this.presenceServiceProxy.SendMessageAsync(new ContactReference(targetContactId, null), "typeTest", JToken.FromObject("Hi !"), default);
                }
                else if (key == 'f')
                {
                    Console.WriteLine();
                    Console.Write("Enter Name to search:");
                    var nameMatch = Console.ReadLine();

                    var searchResult = await this.presenceServiceProxy.SearchContactsAsync(
                        new Dictionary<string, SearchProperty>
                        {
                            {
                                "name", new SearchProperty()
                                {
                                    Expression = $"^{nameMatch}",
                                    Options = (int)RegexOptions.IgnoreCase,
                                }
                            },
                        },
                        null,
                        CancellationToken.None);

                    if (searchResult.Count == 0)
                    {
                        Console.WriteLine("No contacts found");
                    }
                    else
                    {
                        foreach (var match in searchResult)
                        {
                            Console.WriteLine($"Id:{match.Key} properties:{match.Value.ConvertToString(null)}");
                        }
                    }
                }
            }
        }

        private class EchoApp : SignalRApp
        {
            private int echoSecs;

            public EchoApp(int echoSecs)
            {
                this.echoSecs = echoSecs;
            }

            protected override string HubName => "healthhub";

            protected override Task HandleKeyAsync(char key) { return Task.CompletedTask; }

            protected override void OnHubCreated(HubClient hubClient, TraceSource traceSource)
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        if (hubClient.IsConnected)
                        {
                            try
                            {
                                var result = await hubClient.Connection.InvokeAsync<JObject>("Echo", "Hello from CLI", DisposeToken);
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

        private class RelayApp : SignalRApp
        {
            private const string TypeTest = "test";

            private readonly CommandOption userIdOption;
            private RelayServiceProxy relayServiceProxy;
            private string lastHubId;
            private IRelayHubProxy currentRelayHubProxy;

            public RelayApp(CommandOption userIdOption)
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

            protected override void OnHubCreated(HubClient hubClient, TraceSource traceSource)
            {
                this.relayServiceProxy = HubProxy.CreateHubProxy<RelayServiceProxy>(hubClient.Connection, traceSource, true);
            }
        }
    }
}
