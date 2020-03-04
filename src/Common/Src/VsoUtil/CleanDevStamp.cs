// <copyright file="CleanDevStamp.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Cleans up devstamp.
    /// </summary>
    [Verb("cleandevstamp", HelpText = "clean up dev stamp resources.")]
    public class CleanDevStamp : CommandBase
    {
        private const string CloudEnvironmentDbName = "cloud-environments";

        /// <summary>
        /// Gets or sets the user alias.
        /// </summary>
        [Option('a', "alias", Default = null, HelpText = "User alias to clean up.")]
        public string Alias { get; set; }

        /// <summary>
        /// Gets or sets the user alias.
        /// </summary>
        [Option('f', "file", Default = null, HelpText = "Path of the file which has list of alias.")]
        public string ListFile { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteAsync(services, stdout, stderr).Wait();
        }

        // This command is setup only for a developer stamp.
        private async Task ExecuteAsync(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            var azureSubscriptionCatalog = services.GetRequiredService<IAzureSubscriptionCatalog>();
            DeveloperPersonalStampSettings developerPersonalStampSettings = null;

            if (!string.IsNullOrWhiteSpace(ListFile))
            {
                foreach (var item in await File.ReadAllLinesAsync(ListFile))
                {
                    var settings = new DeveloperPersonalStampSettings(true, item.Trim());
                    var resourceNameBuilder = new ResourceNameBuilder(settings);
                    await CleanUpAsync(stdout, stderr, resourceNameBuilder, azureSubscriptionCatalog);
                }
            }
            else if (!string.IsNullOrWhiteSpace(Alias))
            {
                developerPersonalStampSettings = new DeveloperPersonalStampSettings(true, Alias);
            }
            else
            {
                developerPersonalStampSettings = new DeveloperPersonalStampSettings(true, System.Environment.UserName);
            }

            var builder = new ResourceNameBuilder(developerPersonalStampSettings);
            await CleanUpAsync(stdout, stderr, builder, azureSubscriptionCatalog);
        }

        private async Task CleanUpAsync(TextWriter stdout, TextWriter stderr, IResourceNameBuilder resourceNameBuilder, IAzureSubscriptionCatalog azureSubscriptionCatalog)
        {
            var deletionTasks = new List<Task>();

            foreach (var catalog in azureSubscriptionCatalog.AzureSubscriptions)
            {
                var name = "ShouldNotMatter"; // The resource group should not depend on the name passed, but instead on the alias.
                var deletionTask = DeleteResourceGroupAsync(
                    resourceNameBuilder.GetResourceGroupName(name),
                    catalog.SubscriptionId,
                    stdout,
                    stderr);

                deletionTasks.Add(deletionTask);
            }

            await Task.WhenAll(deletionTasks);

            var dbName = resourceNameBuilder.GetCosmosDocDBName(CloudEnvironmentDbName);
            await DeleteDatabaseCollection(dbName, stdout, stderr);
        }

        private Task DeleteResourceGroupAsync(string resourceGroupName, string subscription, TextWriter stdout, TextWriter stderr)
        {
            var azureClientFactory = GetServiceProvider().GetRequiredService<IAzureClientFactory>();

            return Task.Run(async () =>
            {
                try
                {
                    var azure = await azureClientFactory.GetAzureClientAsync(Guid.Parse(subscription));
                    if (await azure.ResourceGroups.ContainAsync(resourceGroupName))
                    {
                        await stdout.WriteLineAsync($"resource group {resourceGroupName} found in subscription {subscription}.");
                        await azure.ResourceGroups.DeleteByNameAsync(resourceGroupName);
                        await stdout.WriteLineAsync($"resource group {resourceGroupName} in subscription {subscription} was successfully deleted.");
                    }
                    else
                    {
                        await stdout.WriteLineAsync($"no resource group {resourceGroupName} found in subscription {subscription}.");
                    }
                }
                catch (Exception e)
                {
                    await stderr.WriteLineAsync($"Error encountered while deleting resource group. Exception {e}");
                }
            });
        }

        private Task DeleteDatabaseCollection(string dbName, TextWriter stdout, TextWriter stderr)
        {
            return Task.Run(async () =>
            {
                var azureClientFactory = GetServiceProvider().GetRequiredService<IAzureClientFactory>();
                var controlPlaneAzureResourceAcccessor = GetServiceProvider().GetRequiredService<IControlPlaneAzureResourceAccessor>();

                var (hostUrl, authKey) = await controlPlaneAzureResourceAcccessor.GetStampCosmosDbAccountAsync();

                using (var cosmosClient = new CosmosClient(
                    hostUrl,
                    authKey,
                    new CosmosClientOptions()
                    {
                        ApplicationRegion = Regions.WestUS2, // TODO: janraj, how to get this?
                    }))
                {
                    var db = cosmosClient.GetDatabase(dbName);
                    await stdout.WriteLineAsync($"Got db {db.Id}");
                    try
                    {
                        await stdout.WriteLineAsync($"Attempting to delete Db {db.Id}.");
                        await db.DeleteAsync();
                        await stdout.WriteLineAsync($"Deleting db {db.Id} was successful.");
                    }
                    catch (CosmosException e)
                    {
                        if (e.StatusCode == HttpStatusCode.NotFound)
                        {
                            await stdout.WriteLineAsync($"db with id {db.Id} was not found.");
                        }
                        else
                        {
                            await stderr.WriteLineAsync($"Deleting DB {db.Id} threw exception. {e}");
                        }
                    }
                }
            });
        }
    }
}
