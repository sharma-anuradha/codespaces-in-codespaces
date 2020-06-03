// <copyright file="CleanDevStamp.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
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
        private const string DiskResourceType = "Microsoft.Compute/disks";

        private static readonly string[] DevStampRegions = { "WestEurope", "WestUS2" };

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

        /// <summary>
        /// Gets or sets a value indicating whether to clean up resources in all regions.
        /// </summary>
        [Option("all", Default = false, HelpText = "Clean up resources in all regions. Overrides -l parameter.")]
        public bool AllRegions { get; set; }

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
                    await CleanUpAsync(resourceNameBuilder, azureSubscriptionCatalog, stdout, stderr);
                }

                return;
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
            await CleanUpAsync(builder, azureSubscriptionCatalog, stdout, stderr);
        }

        private async Task CleanUpAsync(IResourceNameBuilder resourceNameBuilder, IAzureSubscriptionCatalog azureSubscriptionCatalog, TextWriter stdout, TextWriter stderr)
        {
            var azureSubscriptions = azureSubscriptionCatalog.AzureSubscriptionsIncludingInfrastructure();
            if (AllRegions)
            {
                await CleanUpAllRegionsAsync(resourceNameBuilder, azureSubscriptions, stdout, stderr);
            }
            else
            {
                if (DevStampRegions.Contains(Location, StringComparer.InvariantCultureIgnoreCase))
                {
                    await CleanUpRegionAsync(Location, resourceNameBuilder, azureSubscriptions, stdout, stderr);
                }
                else
                {
                    await stderr.WriteLineAsync($"Error: Unsupported devstamp region \"{Location}\" supplied with location parameter.");
                    return;
                }
            }
        }

        private async Task CleanUpAllRegionsAsync(IResourceNameBuilder resourceNameBuilder, IEnumerable<IAzureSubscription> azureSubscriptions, TextWriter stdout, TextWriter stderr)
        {
            await stdout.WriteLineAsync("Cleaning up resources in all devstamp regions...");

            var deletionTasks = new List<Task>();

            foreach (var subscription in azureSubscriptions)
            {
                var name = "ShouldNotMatter"; // The resource group should not depend on the name passed, but instead on the alias.
                var deletionTask = DeleteResourceGroupAsync(
                    resourceNameBuilder.GetResourceGroupName(name),
                    subscription,
                    stdout,
                    stderr);

                deletionTasks.Add(deletionTask);
            }

            await Task.WhenAll(deletionTasks);

            var dbName = resourceNameBuilder.GetCosmosDocDBName(CloudEnvironmentDbName);
            foreach (var region in DevStampRegions)
            {
                await DeleteDatabaseCollectionInRegion(dbName, region, stdout, stderr);
            }
        }

        private async Task CleanUpRegionAsync(string region, IResourceNameBuilder resourceNameBuilder, IEnumerable<IAzureSubscription> azureSubscriptions, TextWriter stdout, TextWriter stderr)
        {
            await stdout.WriteLineAsync($"Cleaning up resources in \"{region}\" devstamp region...");

            var deletionTasks = new List<Task>();

            foreach (var subscription in azureSubscriptions)
            {
                var name = "ShouldNotMatter"; // The resource group should not depend on the name passed, but instead on the alias.
                var deletionTask = DeleteResourcesInRegionAsync(
                    resourceNameBuilder.GetResourceGroupName(name),
                    region,
                    subscription,
                    stdout,
                    stderr);

                deletionTasks.Add(deletionTask);
            }

            await Task.WhenAll(deletionTasks);

            var dbName = resourceNameBuilder.GetCosmosDocDBName(CloudEnvironmentDbName);
            await DeleteDatabaseCollectionInRegion(dbName, region, stdout, stderr);
        }

        private Task DeleteResourceGroupAsync(string resourceGroupName, IAzureSubscription azureSubscription, TextWriter stdout, TextWriter stderr)
        {
            var azureClientFactory = GetServiceProvider().GetRequiredService<IAzureClientFactory>();

            return Task.Run(async () =>
            {
                try
                {
                    var azure = await azureClientFactory.GetAzureClientAsync(Guid.Parse(azureSubscription.SubscriptionId));
                    await stdout.WriteLineAsync($"Checking for resources in resource group {resourceGroupName} in subscription {azureSubscription.DisplayName}...");
                    if (await azure.ResourceGroups.ContainAsync(resourceGroupName))
                    {
                        await stdout.WriteLineAsync($"Resource group {resourceGroupName} found in subscription {azureSubscription.DisplayName}.");
                        await stdout.WriteLineAsync($"Deleting resource group in {azureSubscription.DisplayName}...");
                        await DoWithDryRun(() => azure.ResourceGroups.DeleteByNameAsync(resourceGroupName));
                        await stdout.WriteLineAsync($"Resource group {resourceGroupName} in subscription {azureSubscription.DisplayName} was successfully deleted.");
                    }
                    else
                    {
                        await stdout.WriteLineAsync($"No resource group {resourceGroupName} found in subscription {azureSubscription.DisplayName}.");
                    }
                }
                catch (Exception e)
                {
                    await stderr.WriteLineAsync($"Error encountered while deleting resource group. Exception {e}");
                }
            });
        }

        private Task DeleteResourcesInRegionAsync(string resourceGroupName, string region, IAzureSubscription azureSubscription, TextWriter stdout, TextWriter stderr)
        {
            var azureClientFactory = GetServiceProvider().GetRequiredService<IAzureClientFactory>();

            return Task.Run(async () =>
            {
                try
                {
                    await stdout.WriteLineAsync($"Checking for resources in resource group {resourceGroupName} in subscription {azureSubscription.DisplayName}...");
                    var resourceManagementClient = await azureClientFactory.GetResourceManagementClient(Guid.Parse(azureSubscription.SubscriptionId));
                    var regionResources = await EnumerateResourcesInRegionAsync(resourceGroupName, region, resourceManagementClient, azureSubscription.DisplayName, stdout, stderr);
                    if (regionResources != null)
                    {
                        if (regionResources.Any())
                        {
                            await stdout.WriteLineAsync($"Deleting resources in {azureSubscription.DisplayName}...");
                            var resourceProviders = await resourceManagementClient.Providers.ListAsync();

                            // Remove the disks last, as you cannot delete them before the VM has been cleaned up.
                            foreach (var resource in regionResources.OrderBy(resource => resource.Type == DiskResourceType))
                            {
                                var apiVersion = GetLatestResourceApiVersion(resourceProviders, resource.Type);
                                await TryDeleteResourceAsync(resource, apiVersion, resourceManagementClient, stdout, stderr);
                            }

                            await stdout.WriteLineAsync($"All resources in {azureSubscription.DisplayName} have been deleted.");
                        }
                        else
                        {
                            await stdout.WriteLineAsync($"No resources found in {azureSubscription.DisplayName} in {region} region.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    await stderr.WriteLineAsync($"Subscription {azureSubscription.DisplayName} : Error encountered while deleting resources. Exception {ex}");
                }
            });
        }

        private string GetLatestResourceApiVersion(IEnumerable<ProviderInner> resourceProviders, string resourceType)
        {
            var resource = resourceProviders.First(provider => provider.NamespaceProperty == resourceType.Split("/")[0])
                                            .ResourceTypes.First(type => type.ResourceType == resourceType.Split("/")[1]);
            return resource.ApiVersions.First();
        }

        private async Task<IEnumerable<GenericResourceInner>> EnumerateResourcesInRegionAsync(
            string resourceGroupName,
            string region,
            IResourceManagementClient resourceManagementClient,
            string subscriptionName,
            TextWriter stdout,
            TextWriter stderr)
        {
            IEnumerable<GenericResourceInner> regionResources = null;

            try
            {
                // If the resource group does not exist in the subscription an exception will be thrown.
                var allResources = await resourceManagementClient.Resources.ListByResourceGroupAsync(resourceGroupName);
                regionResources = allResources.Where(resource => resource.Location.Equals(region, StringComparison.OrdinalIgnoreCase));

                regionResources = await EnsureDiskResourceEnumeration(regionResources, resourceGroupName, resourceManagementClient);

                await stdout.WriteLineAsync($"Found {allResources.Count()} resources in {subscriptionName}, {regionResources.Count()} in {region}");
                foreach (var resource in regionResources)
                {
                    await WriteVerboseLineAsync(stdout, $"Found resource \"{resource.Name}\" of type {resource.Type} in {subscriptionName}");
                }
            }
            catch (Exception ex)
            {
                await stderr.WriteLineAsync($"Subscription {subscriptionName} error : {ex.Message}");
            }

            return regionResources;
        }

        /// <summary>
        /// In some cases, ListByResourceGroupAsync will not enumerate the disk resources. This method looks up the resource based on the
        /// naming convention that we use: %vm name%-disk and ensures it is added to the resource list.
        /// </summary>
        private async Task<IEnumerable<GenericResourceInner>> EnsureDiskResourceEnumeration(
            IEnumerable<GenericResourceInner> regionResources,
            string resourceGroupName,
            IResourceManagementClient resourceManagementClient)
        {
            var virtualMachines = regionResources.Where(resource => resource.Type == "Microsoft.Compute/virtualMachines");
            if (virtualMachines.Any())
            {
                var resourceProviders = await resourceManagementClient.Providers.ListAsync();
                var apiVersion = GetLatestResourceApiVersion(resourceProviders, DiskResourceType);

                var disks = new List<GenericResourceInner>(virtualMachines.Count());
                foreach (var virtualMachine in virtualMachines)
                {
                    var diskName = $"{virtualMachine.Name}-disk";
                    if (!regionResources.Any(resource => resource.Name == diskName))
                    {
                        var disk = await resourceManagementClient.Resources.GetAsync(resourceGroupName, "Microsoft.Compute", string.Empty, "disks", diskName, apiVersion);
                        if (disk != null)
                        {
                            disks.Add(disk);
                        }
                    }
                }

                if (disks.Any())
                {
                    var updatedRegionResources = regionResources.ToList();
                    updatedRegionResources.AddRange(disks);
                    regionResources = updatedRegionResources;
                }
            }

            return regionResources;
        }

        private async Task TryDeleteResourceAsync(
            GenericResourceInner resource,
            string apiVersion,
            IResourceManagementClient resourceManagementClient,
            TextWriter stdout,
            TextWriter stderr)
        {
            try
            {
                await WriteVerboseLineAsync(stdout, $"Deleting resource \"{resource.Name}\" of type {resource.Type}...");
                await DoWithDryRun(() => resourceManagementClient.Resources.DeleteByIdAsync(resource.Id, apiVersion));
                await WriteVerboseLineAsync(stdout, $"Resource \"{resource.Name}\" successfully deleted.");
            }
            catch (Exception ex)
            {
                await stderr.WriteLineAsync($"Error encountered while deleting resource \"{resource.Name}\" - {ex.Message}");
            }
        }

        private Task DeleteDatabaseCollectionInRegion(string dbName, string region, TextWriter stdout, TextWriter stderr)
        {
            return Task.Run(async () =>
            {
                var azureClientFactory = GetServiceProvider().GetRequiredService<IAzureClientFactory>();
                var controlPlaneAzureResourceAcccessor = GetServiceProvider().GetRequiredService<IControlPlaneAzureResourceAccessor>();

                var (hostUrl, authKey) = await controlPlaneAzureResourceAcccessor.GetStampCosmosDbAccountAsync();
                var cosmosApplicationRegion = region.ToLower() switch
                {
                    "westeurope" => Regions.WestEurope,
                    "westus2" => Regions.WestUS2,
                    _ => throw new ArgumentException("Invalid region parameter")
                };

                using (var cosmosClient = new CosmosClient(
                    hostUrl,
                    authKey,
                    new CosmosClientOptions()
                    {
                        ApplicationRegion = cosmosApplicationRegion,
                    }))
                {
                    var db = cosmosClient.GetDatabase(dbName);
                    await stdout.WriteLineAsync($"Got db {db.Id} in region \"{cosmosApplicationRegion}\"");
                    try
                    {
                        await stdout.WriteLineAsync($"Attempting to delete Db {db.Id}...");

                        await DoWithDryRun(() => db.DeleteAsync());
                        await stdout.WriteLineAsync($"Deleting db {db.Id} was successful.");
                    }
                    catch (CosmosException e)
                    {
                        if (e.StatusCode == HttpStatusCode.NotFound)
                        {
                            await stdout.WriteLineAsync($"db with id {db.Id} in region \"{cosmosApplicationRegion}\" was not found.");
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
