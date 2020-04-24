// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using CommandLine;
using Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// The catalog command-line untility.
    /// </summary>
    public static partial class Program
    {
        /// <summary>
        /// The main program entry point.
        /// </summary>
        /// <param name="args">The args list.</param>
        /// <returns>The exit code.</returns>
        public static int Main(string[] args)
        {
            try
            {
                return Parser.Default.ParseArguments<
                    ShowDbAccountInfoCommand,
                    CreatePortForwardingConnection,
                    PortForwardingConnectionEstablished,
                    ShowSkusCommand,
                    ShowSubscriptionCommand,
                    ShowControlPlaneCommand,
                    ListPoolsCommand,
                    ListPoolSettingsCommand,
                    ShowPoolSettingsCommand,
                    SetPoolSettingsCommand,
                    DeletePoolSettingsCommand,
                    PrepareDevCLICommand,
                    SetSkuImageVersionCommand,
                    CleanDevStamp,
                    ListDevStamps>(args)
                    .MapResult(
                        (ShowDbAccountInfoCommand command) => command.Execute(Console.Out, Console.Error),
                        (CreatePortForwardingConnection command) => command.Execute(Console.Out, Console.Error),
                        (PortForwardingConnectionEstablished command) => command.Execute(Console.Out, Console.Error),
                        (ShowSkusCommand command) => command.Execute(Console.Out, Console.Error),
                        (ShowSubscriptionCommand command) => command.Execute(Console.Out, Console.Error),
                        (ShowControlPlaneCommand command) => command.Execute(Console.Out, Console.Error),
                        (ListPoolsCommand command) => command.Execute(Console.Out, Console.Error),
                        (ListPoolSettingsCommand command) => command.Execute(Console.Out, Console.Error),
                        (ShowPoolSettingsCommand command) => command.Execute(Console.Out, Console.Error),
                        (SetPoolSettingsCommand command) => command.Execute(Console.Out, Console.Error),
                        (DeletePoolSettingsCommand command) => command.Execute(Console.Out, Console.Error),
                        (PrepareDevCLICommand command) => command.Execute(Console.Out, Console.Error),
                        (SetSkuImageVersionCommand command) => command.Execute(Console.Out, Console.Error),
                        (CleanDevStamp command) => command.Execute(Console.Out, Console.Error),
                        (ListDevStamps command) => command.Execute(Console.Out, Console.Error),
                        errs => 1);
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return 1;
            }
        }

        private static void PrintException(Exception ex)
        {
            if (ex != null)
            {
                if (ex is AggregateException aggregate)
                {
                    foreach (var e in aggregate.InnerExceptions)
                    {
                        PrintException(e);
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"error: {ex.Message}");
                    Console.ResetColor();
                    PrintException(ex.InnerException);
                }
            }
        }
    }
}
