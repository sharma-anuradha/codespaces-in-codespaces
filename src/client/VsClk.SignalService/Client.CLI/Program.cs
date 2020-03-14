// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.CommandLineUtils;

namespace SignalService.Client.CLI
{
    internal class Program : CommandLineApplication
    {
        public CommandOption ServiceEndpointOption { get; private set; }

        public CommandOption AccessTokenOption { get; private set; }

        public CommandOption MessagePackOption { get; private set; }

        public CommandOption DebugSignalROption { get; private set; }

        public CommandOption HubName { get; private set; }

        public CommandOption SkipNegotiate { get; private set; }

        public static int Main(string[] args)
        {
            var runtimeVer = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            Console.WriteLine($"Starting SignalService CLI using runtime version:{runtimeVer}");

            var cli = new Program();

            cli.ServiceEndpointOption = cli.Option(
                "-s | --service",
                "Override the web service endpoint URI",
                CommandOptionType.SingleValue);

            cli.AccessTokenOption = cli.Option(
                "--accessToken",
                "Access token for authentication",
                CommandOptionType.SingleValue);

            cli.MessagePackOption = cli.Option(
                "--messagePack",
                "If message pack is enabled",
                CommandOptionType.NoValue);

            cli.SkipNegotiate = cli.Option(
                "--skipNegotiate",
                "If skip the signalR negotiate",
                CommandOptionType.NoValue);

            cli.DebugSignalROption = cli.Option(
                "--debugSignalR",
                "If SignalR tracing is enabled",
                CommandOptionType.NoValue);

            cli.HubName = cli.Option(
                "--hubName",
                "Name of the hub",
                CommandOptionType.SingleValue);

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

                app.OnExecute(() => new ContactApp(contactIdOption, emailOption, nameOption).RunAsync(cli));
            });

            cli.Command("run-relay", app =>
            {
                var userIdOption = cli.Option(
                    "--userId",
                    "User id to use when joining",
                    CommandOptionType.SingleValue);

                app.OnExecute(() => new RelayHubApp(userIdOption).RunAsync(cli));
            });

            cli.Command("run-presence-stress", app =>
            {
                var contactsFilePathOption = cli.Option(
                    "--contactsFile",
                    "File with contacts info",
                    CommandOptionType.SingleValue);
                app.OnExecute(() => new ContactStressApp(contactsFilePathOption).RunAsync(cli));
            });

            cli.Command("run-relay-stress", app =>
            {
                var serviceEndpoint = cli.Option(
                    "--serviceEndpoint",
                    "An optional service endpoint to use",
                    CommandOptionType.SingleValue);

                var tracehubDataOption = cli.CreateTraceHubDataOption();
                app.OnExecute(() => new RelayStressApp(serviceEndpoint.HasValue() ? serviceEndpoint.Value() : null, tracehubDataOption.HasValue()).RunAsync(cli));
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

        private CommandOption CreateTraceHubDataOption()
        {
            return Option(
                "--traceHubData",
                "If trace hub data is enabled",
                CommandOptionType.NoValue);
        }
    }
}
