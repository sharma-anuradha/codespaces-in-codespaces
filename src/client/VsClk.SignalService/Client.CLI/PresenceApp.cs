using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
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
    internal class PresenceApp : SignalRApp
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

        protected override void OnHubCreated()
        {
            this.presenceServiceProxy = HubProxy.CreateHubProxy<PresenceServiceProxy>(HubClient.Connection, TraceSource, !this.usePresenceHubNameOption.HasValue());

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

            HubClient.ConnectionStateChanged += async (s, e) =>
            {
                if (HubClient.State == HubConnectionState.Connected)
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
                            { "status", "busy" },
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
                            { "status", "available" },
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
            else if (key == 'S')
            {
                Console.WriteLine();
                Console.Write("Enter email contact:");
                var email = Console.ReadLine();
                Console.WriteLine();
                Console.Write($"Enter properties ({string.Join(',', subscribeProperties)}):");
                var line = Console.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    subscribeProperties = line.Split(',');
                }

                var properties = await this.presenceServiceProxy.RequestSubcriptionsAsync(
                    new Dictionary<string, object>[] { new Dictionary<string, object>() { { Properties.Email, email } } },
                    subscribeProperties,
                    useStubContact: true,
                    CancellationToken.None);
                Console.WriteLine($"Request subscription properties:{properties[0].ConvertToString(null)}");
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

}
