// <copyright file="BanSubscriptionCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Ban a subscription.
    /// </summary>
    [Verb("ban-subscription", HelpText = "Ban a subscription.")]
    public class BanSubscriptionCommand : CommandBase
    {
        /// <summary>
        /// Gets or sets a value indicating the subscription to ban.
        /// </summary>
        [Option('s', "subscription", HelpText = "The subscription id.", Required = true)]
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the reason for banning the subscription.
        /// </summary>
        [Option('r', "reason", HelpText = "[ddos, fraud, other]", Required = true)]
        public string Reason { get; set; }

        /// <summary>
        /// Creates the web host.
        /// </summary>
        /// <param name="webHostArgs">The web host arguments.</param>
        /// <returns>The built web host.</returns>
        protected override IWebHost CreateWebHost(string[] webHostArgs)
        {
            var webHost = WebHost.CreateDefaultBuilder(webHostArgs)
                .UseStartup<StartupFrontEnd>()
                .Build();

            StartupFrontEnd.Services = webHost.Services;

            return webHost;
        }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            BannedReason reason;

            if (!Guid.TryParse(SubscriptionId, out var id))
            {
                throw new Exception($"Invalid Subscription ID: {SubscriptionId}");
            }

            if (Reason.Equals("ddos", StringComparison.OrdinalIgnoreCase))
            {
                reason = BannedReason.SuspectedDDOS;
            }
            else if (Reason.Equals("fraud", StringComparison.OrdinalIgnoreCase))
            {
                reason = BannedReason.SuspectedFraud;
            }
            else if (Reason.Equals("other", StringComparison.OrdinalIgnoreCase))
            {
                reason = BannedReason.Other;
            }
            else
            {
                stderr.WriteLine($"Unknown ban reason: {Reason}");
                reason = BannedReason.Other;
            }

            BanSubscriptionAsync(services, id, reason, stdout, stderr).Wait();
        }

        private async Task BanSubscriptionAsync(IServiceProvider services, Guid id, BannedReason reason, TextWriter stdout, TextWriter stderr)
        {
            var superuserIdentity = services.GetRequiredService<VsoSuperuserClaimsIdentity>();
            var currentIdentityProvider = services.GetRequiredService<ICurrentUserProvider>();
            var manager = services.GetRequiredService<ISubscriptionManager>();
            var logger = new NullLogger();

            using (currentIdentityProvider.SetScopedIdentity(superuserIdentity))
            {
                Subscription subscription;

                try
                {
                    subscription = await manager.GetSubscriptionAsync(id.ToString(), logger);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Subscription not found: {SubscriptionId}. {ex.Message}", ex);
                }

                if (Verbose || DryRun)
                {
                    await stdout.WriteLineAsync("Subscription:");
                    await stdout.WriteLineAsync($"  ID: {subscription.Id}");
                    await stdout.WriteLineAsync($"  State: {subscription.SubscriptionState}");
                }

                if (DryRun || subscription.IsBanned)
                {
                    await stdout.WriteLineAsync("Bailing out. " + (subscription.IsBanned ? "Subscription already banned." : "Dry run."));
                    return;
                }

                await manager.AddBannedSubscriptionAsync(id.ToString(), reason, "vsoutil", logger);
            }
        }
    }
}
