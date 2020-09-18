// <copyright file="DeleteEnvironmentCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Delete a Cloud Environment.
    /// </summary>
    [Verb("delete-environment", HelpText = "Delete a Cloud Environment.")]
    public class DeleteEnvironmentCommand : FrontEndCommandBase
    {
        /// <summary>
        /// Gets or sets a value indicating the environment to delete.
        /// </summary>
        [Option('i', "id", HelpText = "The cloud environment id.", Required = true)]
        public string EnvironmentId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the environment should be hard deleted.
        /// </summary>
        [Option("hard", HelpText = "Hard delete the environment.")]
        public bool HardDelete { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            if (!Guid.TryParse(EnvironmentId, out var id))
            {
                throw new Exception($"Invalid Cloud Environment ID: {EnvironmentId}");
            }

            DeleteEnvironmentAsync(services, id, stdout, stderr).Wait();
        }

        private async Task DeleteEnvironmentAsync(IServiceProvider services, Guid id, TextWriter stdout, TextWriter stderr)
        {
            var jobQueueProducerFactory = services.GetRequiredService<IJobQueueProducerFactory>();
            var superuserIdentity = services.GetRequiredService<VsoSuperuserClaimsIdentity>();
            var currentIdentityProvider = services.GetRequiredService<ICurrentUserProvider>();
            var manager = services.GetRequiredService<IEnvironmentManager>();
            var logger = new NullLogger();

            using (currentIdentityProvider.SetScopedIdentity(superuserIdentity))
            {
                CloudEnvironment environment;

                try
                {
                    environment = await manager.GetAsync(id, logger);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Environment not found: {EnvironmentId}. {ex.Message}", ex);
                }

                if (Verbose || DryRun)
                {
                    await stdout.WriteLineAsync("Environment:");
                    await stdout.WriteLineAsync($"  ID: {environment.Id}");
                    await stdout.WriteLineAsync($"  Plan ID: {environment.PlanId}");
                    await stdout.WriteLineAsync($"  Friendly Name: {environment.FriendlyName}");
                    await stdout.WriteLineAsync($"  Created: {environment.Created}");
                    await stdout.WriteLineAsync($"  State: {environment.State}");
                    await stdout.WriteLineAsync($"  Deleted: {environment.IsDeleted}");
                }

                if (DryRun || environment.IsDeleted)
                {
                    await stdout.WriteLineAsync("Bailing out. " + (environment.IsDeleted ? "Environment already deleted." : "Dry run."));
                    return;
                }

                if (HardDelete)
                {
                    await manager.HardDeleteAsync(id, logger);
                }
                else
                {
                    await SoftDeleteEnvironmentJobHandler.ExecuteAsync(jobQueueProducerFactory, environment.Id, environment.Location, logger);
                }
            }
        }
    }
}
