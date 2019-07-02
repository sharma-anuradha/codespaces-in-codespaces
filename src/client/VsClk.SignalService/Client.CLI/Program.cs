using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Client;
using Microsoft.VsCloudKernel.SignalService.Common;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace LivesharePresenceClientTest
{
    class Program : CommandLineApplication
    {
        private CommandOption contactIdOption;
        private CommandOption emailOption;
        private CommandOption nameOption;
        private CommandOption serviceEndpointOption;
        private CommandOption accessTokenOption;
        private CommandOption debugSignalROption;
        private CommandOption echoOption;

        private const string DefaultServiceEndpoint = "https://localhost:5001/presencehub";

        static int Main(string[] args)
        {
            var cli = new Program();

            cli.serviceEndpointOption = cli.Option(
                "-s | --service",
                "Override the web service endpoint URI",
                CommandOptionType.SingleValue);

            cli.contactIdOption = cli.Option(
                "--id",
                "Contact id to use",
                CommandOptionType.SingleValue);

            cli.emailOption = cli.Option(
                "--email",
                "Email to use",
                CommandOptionType.SingleValue);

            cli.nameOption = cli.Option(
                "--name",
                "Name to use",
                CommandOptionType.SingleValue);

            cli.accessTokenOption = cli.Option(
                "--accessToken",
                "Access token for authentication",
                CommandOptionType.SingleValue);

            cli.echoOption = cli.Option(
                "--echo",
                "Perfom echo every (n) secs on the endpoint",
                CommandOptionType.SingleValue);

            cli.debugSignalROption = cli.Option(
                "--debugSignalR",
                "If SignalR tracing is enabled",
                CommandOptionType.NoValue);

            cli.OnExecute(() => cli.ExecuteAsync());

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

        public async Task<int> ExecuteAsync()
        {
            string serviceEndpoint = this.serviceEndpointOption.Value();
            if (string.IsNullOrEmpty(serviceEndpoint))
            {
                serviceEndpoint = DefaultServiceEndpoint;
            }

            string contactId = this.contactIdOption.HasValue() ? this.contactIdOption.Value() : null;
            string email = this.emailOption.HasValue() ? this.emailOption.Value() : null;
            int echoSecs = this.echoOption.HasValue() ? int.Parse(this.echoOption.Value()) : -1;

            var presenceClientTraceSource = CreateTraceSource("PresenceServiceClient");
            presenceClientTraceSource.Verbose($"Started serviceEndpoint:{serviceEndpoint} contactId:{contactId}");

            IHubConnectionBuilder hubConnectionBuilder;
            if (this.accessTokenOption.HasValue())
            {
                hubConnectionBuilder = HubConnectionHelpers.FromUrlAndAccessToken(serviceEndpoint, this.accessTokenOption.Value());
            }
            else
            {
                hubConnectionBuilder = HubConnectionHelpers.FromUrl(serviceEndpoint);
            }

            if (this.debugSignalROption.HasValue())
            {
                hubConnectionBuilder.ConfigureLogging(logging =>
                {
                    // Log to the Console
                    logging.AddConsole();

                    // This will set ALL logging to Debug level
                    logging.SetMinimumLevel(LogLevel.Debug);
                });
            }

            var presenceClient = new HubClientProxy<PresenceServiceProxy>(hubConnectionBuilder.Build(), presenceClientTraceSource);

            presenceClient.Proxy.UpdateProperties += (s, e) =>
            {
                Console.WriteLine($"->UpdateProperties from:{e.Contact} props:{e.Properties.ConvertToString()}");
            };

            presenceClient.Proxy.MessageReceived += (s, e) =>
            {
                Console.WriteLine($"->MessageReceived: from:{e.FromContact} type:{e.Type} body:{e.Body}");
            };

            presenceClient.Proxy.ConnectionChanged += (s, e) =>
            {
                Console.WriteLine($"->ConnectionChanged: from:{e.Contact} changeType:{e.ChangeType}");
            };

            presenceClient.ConnectionStateChanged += async (s, e) =>
            {
                if (presenceClient.State == HubConnectionState.Connected)
                {
                    var publishedProperties = new Dictionary<string, object>()
                    {
                        { "status", "available" }
                    };

                    if (!string.IsNullOrEmpty(email))
                    {
                        publishedProperties["email"] = email;
                    }

                    if (this.nameOption.HasValue())
                    {
                        publishedProperties.Add("name", this.nameOption.Value());
                    }

                    var registerInfo = await presenceClient.Proxy.RegisterSelfContactAsync(contactId, publishedProperties, CancellationToken.None);
                    Console.WriteLine($"register info->{registerInfo.ConvertToString()}");
                }
            };

            var disposeCts = new CancellationTokenSource();
            HubClient echoClient = null;
            if (echoSecs != -1)
            {
                var echoHealthEndpoint = serviceEndpoint.Substring(0, serviceEndpoint.LastIndexOf('/')) + "/healthhub";
                echoClient = new HubClient(echoHealthEndpoint, CreateTraceSource("EchoHubClient"));
                echoClient.StartAsync(disposeCts.Token).Forget();
                Task.Run(async () =>
                {
                    while(true)
                    {
                        if (echoClient.IsConnected)
                        {
                            try
                            {
                                var result = await echoClient.Connection.InvokeAsync<JObject>("Echo", "Hello from CLI", disposeCts.Token);
                                Console.WriteLine($"Succesfully received echo -> result:{result.ToString()}");
                            }
                            catch (Exception err)
                            {
                                Console.WriteLine($"Failed to echo -> err:{err}");
                            }
                        }

                        await Task.Delay(TimeSpan.FromSeconds(echoSecs), disposeCts.Token);
                    }
                }).Forget();
            }

            if (echoSecs == -1 && string.IsNullOrEmpty(contactId))
            {
                Console.WriteLine("No hub client connection to start, quitting...(specify --id or --echo [secs])");
                return -1;
            }

            if (!string.IsNullOrEmpty(contactId) || this.accessTokenOption.HasValue())
            {
                await presenceClient.StartAsync(CancellationToken.None);
            }

            var subscribeProperties = new string[] { "status", "email" };
            var publishProperty = "status";

            Console.WriteLine("Accepting key options...");
            while (true)
            {
                var key = Console.ReadKey();
                Console.WriteLine($"Option:{key.KeyChar} selected");
                if (key.KeyChar == 'q')
                {
                    disposeCts.Cancel();
                    await presenceClient.StopAsync(CancellationToken.None);
                    if (echoClient != null)
                    {
                        await echoClient.StopAsync(CancellationToken.None);
                    }
                    break;
                }
                else if (!presenceClient.IsConnected)
                {
                    Console.WriteLine("Waiting for Connection...");
                }
                else if(string.IsNullOrEmpty(contactId))
                {
                    Console.WriteLine("Presence Client not register -> specify '--id' opion");
                }
                else if (key.KeyChar == 'b')
                {
                    Console.WriteLine("Changing status to 'busy'");
                    var updateValues = new Dictionary<string, object>()
                        {
                            {"status", "busy" }
                        };

                    await presenceClient.Proxy.PublishPropertiesAsync(updateValues, CancellationToken.None);
                }
                else if (key.KeyChar == 'p')
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
                            { publishProperty, jTokenValue }
                        };

                            await presenceClient.Proxy.PublishPropertiesAsync(updateValues, CancellationToken.None);
                        }
                        catch(JsonException jsonExcp)
                        {
                            Console.Write($"Failed to parse value err:{jsonExcp.Message}");
                        }
                    }
                }
                else if (key.KeyChar == 'a')
                {
                    Console.WriteLine("Changing status to 'available'");
                    var updateValues = new Dictionary<string, object>()
                        {
                            {"status", "available" }
                        };

                    await presenceClient.Proxy.PublishPropertiesAsync(updateValues, CancellationToken.None);
                }
                else if (key.KeyChar == 'c')
                {
                    Console.WriteLine();
                    Console.Write("Enter contact id:");
                    var selfContactId = Console.ReadLine();

                    var selfConnections = await presenceClient.Proxy.GetSelfConnectionsAsync(selfContactId, CancellationToken.None);
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
                else if (key.KeyChar == 's')
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

                    var properties = await presenceClient.Proxy.AddSubcriptionsAsync(
                        new ContactReference[] { new ContactReference(subscribeContactId, null) },
                        subscribeProperties,
                        CancellationToken.None);
                    Console.WriteLine($"Add subscription for id:{subscribeContactId} properties:{string.Join(',', subscribeProperties)} result:{properties[subscribeContactId].ConvertToString()}");
                }
                else if (key.KeyChar == 'u')
                {
                    Console.WriteLine();
                    Console.Write("Enter subscription contact id:");
                    var targetContactId = Console.ReadLine();

                    await presenceClient.Proxy.RemoveSubscriptionAsync(
                        new ContactReference[] { new ContactReference(targetContactId, null) },
                        CancellationToken.None);
                }
                else if (key.KeyChar == 'm')
                {
                    Console.WriteLine();
                    Console.Write("Enter email to match:");
                    var emailMatch = Console.ReadLine();

                    var matchingContacts = await presenceClient.Proxy.MatchContactsAsync(new Dictionary<string, object>[] { new Dictionary<string, object>() { { "email", emailMatch } } }, CancellationToken.None);
                    if(matchingContacts[0].Count == 0)
                    {
                        Console.WriteLine("No match found");
                    }
                    else
                    {
                        foreach(var matchKvp in matchingContacts[0])
                        {
                            Console.WriteLine($"Id:{matchKvp.Key} properties:{matchKvp.Value.ConvertToString()}");
                        }
                    }
                }
                else if (key.KeyChar == 'x')
                {
                    Console.WriteLine();
                    Console.Write("Enter target contact id:");
                    var targetContactId = Console.ReadLine();

                    await presenceClient.Proxy.SendMessageAsync(new ContactReference(targetContactId, null), "typeTest", JToken.FromObject("Hi !"), default);
                }
                else if (key.KeyChar == 'f')
                {
                    Console.WriteLine();
                    Console.Write("Enter Name to search:");
                    var nameMatch = Console.ReadLine();

                    var searchResult = await presenceClient.Proxy.SearchContactsAsync(new Dictionary<string, SearchProperty>
                    {
                        {
                            "name", new SearchProperty()
                            {
                                Expression = $"^{nameMatch}",
                                Options = (int)(RegexOptions.IgnoreCase)
                            }
                        },
                    }, null, CancellationToken.None);

                    if (searchResult.Count == 0)
                    {
                        Console.WriteLine("No contacts found");
                    }
                    else
                    {
                        foreach (var match in searchResult)
                        {
                            Console.WriteLine($"Id:{match.Key} properties:{match.Value.ConvertToString()}");
                        }
                    }
                }
            }

            return 0;
        }

        private TraceSource CreateTraceSource(string name)
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
