// <copyright file="DisableBillingCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Delete a Cloud Environment.
    /// </summary>
    [Verb("disable-billing", HelpText = "Disable billing for a plan or subscription.")]
    public class DisableBillingCommand : CommandBase
    {
        /// <summary>
        /// Gets or sets a value indicating the plan to disable billing for.
        /// </summary>
        [Option('p', "plan", HelpText = "The plan id (GUID). Either the plan id or the subscription id must be specified.")]
        public string PlanId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the subscription to disable billing for.
        /// </summary>
        [Option('s', "subscription", HelpText = "The subscription id. Either the plan id or the subscription id must be specified.")]
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the amount of time to disable billing.
        /// </summary>
        [Option('d', "duration", HelpText = "The amount of time to disable billing in 'hh:mm:ss' format. The override will start from the start of the current hour.", Required = true)]
        public string Duration { get; set; }

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
            if (!TimeSpan.TryParse(Duration, out var duration) || duration.TotalMilliseconds <= 0)
            {
                stderr.WriteLine($"Invalid duration: {Duration}");
                return;
            }

            var now = DateTime.UtcNow;
            var startTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
            var endTime = startTime.Add(duration);

            if (!string.IsNullOrEmpty(SubscriptionId))
            {
                if (!Guid.TryParse(SubscriptionId, out var id))
                {
                    stderr.WriteLine($"Invalid Subscription ID: {SubscriptionId}");
                    return;
                }

                DisableBillingForSubscriptionAsync(services, id, startTime, endTime, stdout, stderr).Wait();
            }
            else if (!string.IsNullOrEmpty(PlanId))
            {
                if (!Guid.TryParse(PlanId, out var id))
                {
                    stderr.WriteLine($"Invalid Plan ID: {PlanId}");
                    return;
                }

                DisableBillingForPlanAsync(services, id, startTime, endTime, stdout, stderr).Wait();
            }
            else
            {
                stderr.WriteLine("No plan or subscription specified.");
            }
        }

        private async Task DisableBillingForSubscriptionAsync(IServiceProvider services, Guid id, DateTime startTime, DateTime endTime, TextWriter stdout, TextWriter stderr)
        {
            var superuserIdentity = services.GetRequiredService<VsoSuperuserClaimsIdentity>();
            var currentIdentityProvider = services.GetRequiredService<ICurrentUserProvider>();
            var repository = services.GetRequiredService<IBillingOverrideRepository>();
            var manager = services.GetRequiredService<ISubscriptionManager>();
            var logger = new NullLogger();

            using (currentIdentityProvider.SetScopedIdentity(superuserIdentity))
            {
                Subscription subscription;

                try
                {
                    subscription = await manager.GetSubscriptionAsync(id.ToString(), logger);
                }
                catch
                {
                    await stderr.WriteLineAsync($"Subscription not found: {SubscriptionId}");
                    return;
                }

                if (Verbose || DryRun)
                {
                    await stdout.WriteLineAsync("Subscription:");
                    await stdout.WriteLineAsync($"  ID: {subscription.Id}");
                    await stdout.WriteLineAsync($"  State: {subscription.SubscriptionState}");
                    await stdout.WriteLineAsync();
                    await stdout.WriteLineAsync($"  Disabling billing from {startTime} until {endTime}");
                }

                if (DryRun)
                {
                    await stdout.WriteLineAsync("Bailing out. Dry run.");
                    return;
                }

                var billingOverride = new BillingOverride()
                {
                    BillingOverrideState = BillingOverrideState.BillingDisabled,
                    Id = Guid.NewGuid().ToString(),
                    Subscription = id.ToString(),
                    StartTime = startTime,
                    EndTime = endTime,
                };

                await repository.CreateAsync(billingOverride, logger);

                await stdout.WriteLineAsync($"Billing override ID: {billingOverride.Id}");
            }
        }

        private async Task DisableBillingForPlanAsync(IServiceProvider services, Guid id, DateTime startTime, DateTime endTime, TextWriter stdout, TextWriter stderr)
        {
            var superuserIdentity = services.GetRequiredService<VsoSuperuserClaimsIdentity>();
            var currentIdentityProvider = services.GetRequiredService<ICurrentUserProvider>();
            var repository = services.GetRequiredService<IBillingOverrideRepository>();
            var planRepository = services.GetRequiredService<IPlanRepository>();
            var logger = new NullLogger();

            using (currentIdentityProvider.SetScopedIdentity(superuserIdentity))
            {
                VsoPlan plan;

                try
                {
                    plan = (await planRepository.GetWhereAsync(x => x.Id == id.ToString(), logger, null)).Single();
                }
                catch
                {
                    await stderr.WriteLineAsync($"Plan not found: {id}");
                    return;
                }

                if (Verbose || DryRun)
                {
                    await stdout.WriteLineAsync("Plan:");
                    await stdout.WriteLineAsync($"  ID: {plan.Id}");
                    await stdout.WriteLineAsync($"  Name: {plan.Plan.Name}");
                    await stdout.WriteLineAsync($"  Subscription: {plan.Plan.Subscription}");
                    await stdout.WriteLineAsync($"  Resource Group: {plan.Plan.ResourceGroup}");
                    await stdout.WriteLineAsync($"  Location: {plan.Plan.Location}");
                    await stdout.WriteLineAsync($"  User ID: {plan.UserId}");
                    await stdout.WriteLineAsync($"  Partner: {plan.Partner}");
                    await stdout.WriteLineAsync();
                    await stdout.WriteLineAsync($"  Disabling billing from {startTime} until {endTime}");
                }

                if (DryRun)
                {
                    await stdout.WriteLineAsync("Bailing out. Dry run.");
                    return;
                }

                var billingOverride = new BillingOverride()
                {
                    BillingOverrideState = BillingOverrideState.BillingDisabled,
                    Subscription = plan.Plan.Subscription,
                    Id = Guid.NewGuid().ToString(),
                    PlanId = Guid.Parse(plan.Id),
                    Plan = plan.Plan,
                    StartTime = startTime,
                    EndTime = endTime,
                };

                await repository.CreateAsync(billingOverride, logger);

                await stdout.WriteLineAsync($"Billing override ID: {billingOverride.Id}");
            }
        }
    }
}
