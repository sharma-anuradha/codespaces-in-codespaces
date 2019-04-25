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
using Newtonsoft.Json.Linq;

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

            string contactId = this.contactIdOption.Value();
            string email = this.emailOption.Value();

            var traceSource = new TraceSource("PresenceServiceClient");
            var consoleTraceListener = new ConsoleTraceListener();
            traceSource.Listeners.Add(consoleTraceListener);
            traceSource.Switch.Level = SourceLevels.All;

            traceSource.Verbose($"Started serviceEndpoint:{serviceEndpoint} contactId:{contactId}");

            IHubConnectionBuilder hubConnectionBuilder;
            if (this.accessTokenOption.HasValue())
            {
                hubConnectionBuilder = HubClient.FromUrlAndAccessToken(serviceEndpoint, this.accessTokenOption.Value());
            }
            else
            {
                hubConnectionBuilder = HubClient.FromUrl(serviceEndpoint);
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

            var client = new HubClientProxy<PresenceServiceProxy>(hubConnectionBuilder.Build(), traceSource);

            client.Proxy.UpdateProperties += (s, e) =>
            {
                Console.WriteLine($"->UpdateProperties from:{e.ContactId} props:{e.Properties.ConvertToString()}");
            };

            client.Proxy.MessageReceived += (s, e) =>
            {
                Console.WriteLine($"->MessageReceived: from:{e.FromContactId} type:{e.Type} body:{e.Body}");
            };

            client.ConnectionStateChanged += async (s, e) =>
            {
                if (client.State == HubConnectionState.Connected && !string.IsNullOrEmpty(contactId))
                {
                    var publishedProperties = new Dictionary<string, object>()
                    {
                        { "email", email },
                        { "status", "available" }
                    };

                    if (this.nameOption.HasValue())
                    {
                        publishedProperties.Add("name", this.nameOption.Value());
                    }

                    await client.Proxy.RegisterSelfContactAsync(contactId, publishedProperties, CancellationToken.None);
                }
            };

            await client.StartAsync(CancellationToken.None);
            Console.WriteLine("Accepting key options...");
            while (true)
            {
                var key = Console.ReadKey();
                Console.WriteLine($"Option:{key.KeyChar} selected");
                if (!client.IsConnected)
                {
                    Console.WriteLine("Waiting for Connection...");
                }
                else if (key.KeyChar == 'b')
                {
                    Console.WriteLine("Changing status to 'busy'");
                    var updateValues = new Dictionary<string, object>()
                        {
                            {"status", "busy" }
                        };

                    await client.Proxy.PublishPropertiesAsync(updateValues, CancellationToken.None);
                }
                else if (key.KeyChar == 'a')
                {
                    Console.WriteLine("Changing status to 'available'");
                    var updateValues = new Dictionary<string, object>()
                        {
                            {"status", "available" }
                        };

                    await client.Proxy.PublishPropertiesAsync(updateValues, CancellationToken.None);
                }
                else if (key.KeyChar == 's')
                {
                    Console.WriteLine();
                    Console.Write("Enter subscription contact id:");
                    var subscribeContactId = Console.ReadLine();

                    var properties = await client.Proxy.AddSubcriptionsAsync(new string[] { subscribeContactId }, new string[] { "status", "email" }, CancellationToken.None);
                    Console.WriteLine($"properties => {properties[subscribeContactId].ConvertToString()}");
                }
                else if (key.KeyChar == 'u')
                {
                    Console.WriteLine();
                    Console.Write("Enter subscription contact id:");
                    var targetContactId = Console.ReadLine();

                    await client.Proxy.RemoveSubcriptionPropertiesAsync(new string[] { targetContactId }, new string[] { "status", "email" }, CancellationToken.None);
                }
                else if (key.KeyChar == 'm')
                {
                    Console.WriteLine();
                    Console.Write("Enter email to match:");
                    var emailMatch = Console.ReadLine();

                    var matchingContacts = await client.Proxy.MatchContactsAsync(new Dictionary<string, object>[] { new Dictionary<string, object>() { { "email", emailMatch } } }, CancellationToken.None);
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

                    await client.Proxy.SendMessageAsync(targetContactId, "typeTest", JToken.FromObject("Hi !"), default);
                }
                else if (key.KeyChar == 'f')
                {
                    Console.WriteLine();
                    Console.Write("Enter Name to search:");
                    var nameMatch = Console.ReadLine();

                    var searchResult = await client.Proxy.SearchContactsAsync(new Dictionary<string, SearchProperty>
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
