// <copyright file="Program.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using CommandLine;
using Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil;
using Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.BillingPlans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// The catalog command-line utility.
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
            var exitCode = 0;

            try
            {
                Parser.Default.ParseArguments(
                    args,
                    typeof(ShowDbAccountInfoCommand),
                    typeof(CreatePortForwardingConnection),
                    typeof(PortForwardingConnectionEstablished),
                    typeof(PortForwardingDrainQueues),
                    typeof(ShowSkusCommand),
                    typeof(ShowSubscriptionCommand),
                    typeof(ShowControlPlaneCommand),
                    typeof(ListPoolsCommand),
                    typeof(ListPoolSettingsCommand),
                    typeof(ShowPoolSettingsCommand),
                    typeof(SetPoolSettingsCommand),
                    typeof(DeletePoolSettingsCommand),
                    typeof(PrepareDevCLICommand),
                    typeof(SetSkuImageVersionCommand),
                    typeof(CleanDevStamp),
                    typeof(ListDevStamps),
                    typeof(GetSkuImageVersionCommand),
                    typeof(GetSkuVsChannelUrlCommand),
                    typeof(GetSkuVsVersionCommand),
                    typeof(CleanResourceGroups),
                    typeof(ManagePreviewUsersCommand),
                    typeof(ManageBillingPlansCommand),
                    typeof(DeleteEnvironmentCommand),
                    typeof(SuspendEnvironmentCommand),
                    typeof(BanSubscriptionCommand),
                    typeof(DeletePlanCommand),
                    typeof(DisableBillingCommand),
                    typeof(FeatureToggleCommand),
                    typeof(UpdateConfigSettingCommand),
                    typeof(UpdateQuotaSettingCommand),
                    typeof(EnableSubscriptionSettingCommand),
                    typeof(ConfigurationWriterCommand))
                .WithParsed<ShowDbAccountInfoCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<CreatePortForwardingConnection>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<PortForwardingConnectionEstablished>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<PortForwardingDrainQueues>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<ShowSkusCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<ShowSubscriptionCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<ShowControlPlaneCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<ListPoolsCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<ListPoolSettingsCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<ShowPoolSettingsCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<SetPoolSettingsCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<DeletePoolSettingsCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<PrepareDevCLICommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<SetSkuImageVersionCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<CleanDevStamp>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<ListDevStamps>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<GetSkuImageVersionCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<GetSkuVsChannelUrlCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<GetSkuVsVersionCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<CleanResourceGroups>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<ManagePreviewUsersCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<ManageBillingPlansCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<DeleteEnvironmentCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<SuspendEnvironmentCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<BanSubscriptionCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<DeletePlanCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<DisableBillingCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<FeatureToggleCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<UpdateConfigSettingCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<UpdateQuotaSettingCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<EnableSubscriptionSettingCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithParsed<ConfigurationWriterCommand>(command => command.Execute(Console.Out, Console.Error))
                .WithNotParsed(errs => { exitCode = 1; });
            }
            catch (Exception ex)
            {
                PrintException(ex);
                exitCode = 1;
            }

            return exitCode;
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
                    Console.Error.WriteLine($"error: {ex.ToString()}");
                    Console.ResetColor();
                    PrintException(ex.InnerException);
                }
            }
        }
    }
}
