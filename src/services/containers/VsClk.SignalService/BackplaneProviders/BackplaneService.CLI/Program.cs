// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService;
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;

namespace Microsoft.VsCloudKernel.BackplaneService.CLI
{
    internal class Program : CommandLineApplication
    {
        private const int BackplanePort = 3150;

        public CommandOption NoMessagePackOption { get; private set; }

        public CommandOption HostServiceIdOption { get; private set; }

        public static int Main(string[] args)
        {
            var runtimeVer = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            Console.WriteLine($"Starting SignalService CLI using runtime version:{runtimeVer}");

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole();
            });
            var logger = new Logger<Program>(loggerFactory);

            var cli = new Program();
            cli.NoMessagePackOption = cli.Option(
                "--noMessagePack",
                "If message pack is enabled",
                CommandOptionType.NoValue);
            cli.HostServiceIdOption = cli.Option(
                "--hostServiceId",
                "Define a host service id",
                CommandOptionType.SingleValue);

            var hostServiceIdValue = new Lazy<string>(() =>
            {
                if (cli.HostServiceIdOption.HasValue())
                {
                    return cli.HostServiceIdOption.Value();
                }

                return Guid.NewGuid().ToString();
            });

            Func<CancellationToken, Task<ContactBackplaneServiceProvider>> contactBackplaneServiceProviderFactory = async (ct) =>
            {
                var jsonRpcConnectorProvider = new JsonRpcConnectorProvider("localhost", BackplanePort, !cli.NoMessagePackOption.HasValue(), logger);
                var contactBackplaneServiceProvider = new ContactBackplaneServiceProvider(jsonRpcConnectorProvider, hostServiceIdValue.Value, logger, ct);

                await jsonRpcConnectorProvider.AttemptConnectAsync(ct);
                await jsonRpcConnectorProvider.InvokeAsync<object>("RegisterService", new object[] { "contacts", hostServiceIdValue.Value }, ct);
                return contactBackplaneServiceProvider;
            };

            cli.Command("run-update-batch", app =>
            {
                var numberOfContacts = cli.Option(
                "--n",
                "Number (n) of contacts to upload",
                CommandOptionType.SingleValue);
                app.Description = "Upload contacts to the backplane service";
                app.OnExecute(async () => await UploadContactsAsync(
                    await contactBackplaneServiceProviderFactory(default),
                    hostServiceIdValue.Value,
                    int.Parse(numberOfContacts.Value()),
                    default));
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

        private static async Task<int> UploadContactsAsync(
            ContactBackplaneServiceProvider contactBackplaneServiceProvider,
            string hostServiceId,
            int numberOfContacts,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < numberOfContacts; ++i)
            {
                var changeId = Guid.NewGuid().ToString();
                var contactId = Guid.NewGuid().ToString();
                var connProperties = new Dictionary<string, PropertyValue>();
                var now = DateTime.Now;

                connProperties["status"] = new PropertyValue("available", now);
                connProperties["name"] = new PropertyValue("John Doe", now);
                connProperties["avatar"] = new PropertyValue("http://github.com/avatar", now);
                connProperties["email"] = new PropertyValue($"{contactId}@gmail.com", now);

                var contactDataChanged = new ContactDataChanged<ConnectionProperties>(changeId, hostServiceId, "conn-1", contactId, ContactUpdateType.Registration, connProperties);
                await contactBackplaneServiceProvider.UpdateContactAsync(contactDataChanged, cancellationToken);
                Console.WriteLine($"Upload n:{i} contact:{contactId}");

                if ((i % 100) == 0)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            return 0;
        }
    }
}
