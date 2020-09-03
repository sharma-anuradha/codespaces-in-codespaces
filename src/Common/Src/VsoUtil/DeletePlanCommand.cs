// <copyright file="DeletePlanCommand.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Delete a Codespace Plan.
    /// </summary>
    [Verb("delete-plan", HelpText = "Delete a Codespace Plan.")]
    public class DeletePlanCommand : CommandBase
    {
        /// <summary>
        /// Gets or sets a value indicating the plan to delete.
        /// </summary>
        [Option('p', "plan", HelpText = "The plan id (GUID).", Required = true)]
        public string PlanId { get; set; }

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
            if (string.IsNullOrEmpty(PlanId) || !Guid.TryParse(PlanId, out var id))
            {
                throw new Exception($"Invalid Plan ID: {PlanId}");
            }

            DeletePlanAsync(services, id, stdout, stderr).Wait();
        }

        private async Task DeletePlanAsync(IServiceProvider services, Guid id, TextWriter stdout, TextWriter stderr)
        {
            var superuserIdentity = services.GetRequiredService<VsoSuperuserClaimsIdentity>();
            var currentIdentityProvider = services.GetRequiredService<ICurrentUserProvider>();
            var repository = services.GetRequiredService<IPlanRepository>();
            var manager = services.GetRequiredService<IPlanManager>();
            var logger = new NullLogger();

            using (currentIdentityProvider.SetScopedIdentity(superuserIdentity))
            {
                VsoPlan plan;

                try
                {
                    plan = (await repository.GetWhereAsync(x => x.Id == id.ToString(), logger, null)).Single();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Plan not found: {id}. {ex.Message}", ex);
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
                    await stdout.WriteLineAsync($"  Deleted: {plan.IsDeleted}");
                }

                if (DryRun || plan.IsDeleted)
                {
                    await stdout.WriteLineAsync("Bailing out. " + (plan.IsDeleted ? "Plan already deleted." : "Dry run."));
                    return;
                }

                await manager.DeleteAsync(plan, logger);
            }
        }
    }
}
