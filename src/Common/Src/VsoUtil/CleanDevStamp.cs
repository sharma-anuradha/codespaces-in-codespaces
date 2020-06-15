// <copyright file="CleanDevStamp.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;
using ControlPlaneSettings = Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts.ControlPlaneSettings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Cleans up devstamp.
    /// </summary>
    [Verb("cleandevstamp", HelpText = "clean up dev stamp resources.")]
    public class CleanDevStamp : CommandBase
    {
        private const string CloudEnvironmentDbName = "cloud-environments";
        private const string VirtualMachineResourceType = "Microsoft.Compute/virtualMachines";
        private const string DiskResourceType = "Microsoft.Compute/disks";
        private const string NetworkInterfaceResourceType = "Microsoft.Network/networkInterfaces";
        private const string NetworkSecurityGroupResourceType = "Microsoft.Network/networkSecurityGroups";
        private const string VirtualNetworksResourceType = "Microsoft.Network/virtualNetworks";

        private static readonly string[] DevStampRegions = { "WestEurope", "WestUs2" };
        private static readonly (string Type, string Suffix)[] VirtualMachineResourceTypes =
            new[]
            {
                (DiskResourceType, "-disk"),
                (NetworkInterfaceResourceType, "-nic"),
                (NetworkSecurityGroupResourceType, "-nsg"),
                (VirtualNetworksResourceType, "-vnet"),
            };

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

        /// <summary>
        /// Gets or sets a value indicating whether to double check resource cleanup.
        /// </summary>
        [Option("double-check", Default = false, HelpText = "Double checks region specific resource cleanup by doing a second pass.")]
        public bool DoubleCheck { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteAsync(services, stdout, stderr).Wait();
        }

        private static string GetPrettyDurationFormat(TimeSpan span)
        {
            if (span == TimeSpan.Zero)
            {
                return "0 minutes";
            }

            var sb = new StringBuilder();
            if (span.Days > 0)
            {
                sb.AppendFormat("{0} day{1} ", span.Days, span.Days > 1 ? "s" : string.Empty);
            }

            if (span.Hours > 0)
            {
                sb.AppendFormat("{0} hour{1} ", span.Hours, span.Hours > 1 ? "s" : string.Empty);
            }

            if (span.Minutes > 0)
            {
                sb.AppendFormat("{0} minute{1} ", span.Minutes, span.Minutes > 1 ? "s" : string.Empty);
            }

            if (span.Seconds > 0)
            {
                sb.AppendFormat("{0} second{1} ", span.Seconds, span.Seconds > 1 ? "s" : string.Empty);
            }

            return sb.ToString();
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

        private async Task CleanUpAsync(
            IResourceNameBuilder resourceNameBuilder,
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            TextWriter stdout,
            TextWriter stderr)
        {
            var stopwatch = Stopwatch.StartNew();

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
                }
            }

            stopwatch.Stop();
            await stdout.WriteLineAsync($"Cleanup completed in {GetPrettyDurationFormat(stopwatch.Elapsed)}");
        }

        private async Task CleanUpAllRegionsAsync(
            IResourceNameBuilder resourceNameBuilder,
            IEnumerable<IAzureSubscription> azureSubscriptions,
            TextWriter stdout,
            TextWriter stderr)
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

            foreach (var region in DevStampRegions)
            {
                await DeleteDatabaseCollectionInRegion(region, resourceNameBuilder, stdout, stderr);
            }
        }

        private async Task CleanUpRegionAsync(
            string region,
            IResourceNameBuilder resourceNameBuilder,
            IEnumerable<IAzureSubscription> azureSubscriptions,
            TextWriter stdout,
            TextWriter stderr)
        {
            await stdout.WriteLineAsync($"Cleaning up resources in \"{region}\" devstamp region...");

            var deletionTasks = new List<Task>();

            foreach (var subscription in azureSubscriptions)
            {
                var deletionTask = DeleteResourcesInRegionAsync(
                    region,
                    resourceNameBuilder,
                    subscription,
                    stdout,
                    stderr);

                deletionTasks.Add(deletionTask);
            }

            await Task.WhenAll(deletionTasks);

            await DeleteDatabaseCollectionInRegion(region, resourceNameBuilder, stdout, stderr);
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

        private Task DeleteResourcesInRegionAsync(string region, IResourceNameBuilder resourceNameBuilder, IAzureSubscription azureSubscription, TextWriter stdout, TextWriter stderr)
        {
            var azureClientFactory = GetServiceProvider().GetRequiredService<IAzureClientFactory>();

            return Task.Run(async () =>
            {
                try
                {
                    var resourceGroupName = resourceNameBuilder.GetResourceGroupName(string.Empty);
                    var resourceManagementClient = await azureClientFactory.GetResourceManagementClient(Guid.Parse(azureSubscription.SubscriptionId));

                    await stdout.WriteLineAsync($"Checking for resources in resource group {resourceGroupName} in subscription {azureSubscription.DisplayName}...");
                    var allRegionResources = await EnumerateResourcesInRegionAsync(region, resourceGroupName, resourceManagementClient, azureSubscription, stdout, stderr);
                    if (allRegionResources.Any())
                    {
                        await stdout.WriteLineAsync($"Deleting resources in {azureSubscription.DisplayName}...");

                        // First enumerate and delete any virtual machine based resources
                        var virtualMachineResources = await EnumerateVirtualMachineResources(allRegionResources, resourceGroupName, resourceManagementClient, stdout, stderr);
                        if (virtualMachineResources.Any())
                        {
                            await DeleteVirtualMachineResources(virtualMachineResources, region, resourceNameBuilder, azureSubscription, stdout, stderr);
                        }

                        // Secondly delete any resources that are not virtual machine specific
                        var otherResources = allRegionResources.Except(virtualMachineResources);
                        if (otherResources.Any())
                        {
                            await DeleteResources(otherResources, resourceManagementClient, stdout, stderr);
                        }

                        if (DoubleCheck)
                        {
                            await DoubleCheckResourceCleanup(region, resourceGroupName, resourceManagementClient, azureSubscription, stdout, stderr);
                        }

                        await stdout.WriteLineAsync($"All resources in {azureSubscription.DisplayName} have been deleted.");
                    }
                    else
                    {
                        await stdout.WriteLineAsync($"No resources found in {azureSubscription.DisplayName} in {region} region.");
                    }
                }
                catch (Exception ex)
                {
                    await stderr.WriteLineAsync($"Subscription {azureSubscription.DisplayName} : Error encountered while deleting resources. Exception {ex}");
                }
            });
        }

        private async Task DoubleCheckResourceCleanup(
            string region,
            string resourceGroupName,
            IResourceManagementClient resourceManagementClient,
            IAzureSubscription azureSubscription,
            TextWriter stdout,
            TextWriter stderr)
        {
            await stdout.WriteLineAsync($"Double-checking for resources in resource group {resourceGroupName} in subscription {azureSubscription.DisplayName}...");
            var doubleCheckedResources = await EnumerateResourcesInRegionAsync(region, resourceGroupName, resourceManagementClient, azureSubscription, stdout, stderr);
            if (doubleCheckedResources.Any())
            {
                await stdout.WriteLineAsync($"WARNING: The following resources were found after double-checking resource cleanup in {azureSubscription.DisplayName}:");
                foreach (var resource in doubleCheckedResources)
                {
                    await stdout.WriteLineAsync($"WARNING: Found resource \"{resource.Name}\" of type {resource.Type} in {azureSubscription.DisplayName} during double-check");
                }

                await stdout.WriteLineAsync($"WARNING: Deleting double-checked resources in {azureSubscription.DisplayName}...");
                await DeleteResources(doubleCheckedResources, resourceManagementClient, stdout, stderr);
                await stdout.WriteLineAsync($"Finished deleting after double-checking in subscription {azureSubscription.DisplayName}.");
            }
        }

        private string GetLatestResourceApiVersion(IEnumerable<ProviderInner> resourceProviders, string resourceType)
        {
            var resourceTypeComponents = GetResourceTypeComponents(resourceType);
            var resource = resourceProviders.First(provider => provider.NamespaceProperty == resourceTypeComponents.Namespace)
                                            .ResourceTypes.First(type => type.ResourceType == resourceTypeComponents.Type);
            return resource.ApiVersions.First();
        }

        private (string Namespace, string Type) GetResourceTypeComponents(string resourceType)
        {
            var resourceDetails = resourceType.Split("/");
            return (resourceDetails[0], resourceDetails[1]);
        }

        private async Task<IEnumerable<GenericResourceInner>> EnumerateResourcesInRegionAsync(
            string region,
            string resourceGroupName,
            IResourceManagementClient resourceManagementClient,
            IAzureSubscription azureSubscription,
            TextWriter stdout,
            TextWriter stderr)
        {
            IEnumerable<GenericResourceInner> regionResources = new List<GenericResourceInner>();

            try
            {
                // If the resource group does not exist in the subscription an exception will be thrown.
                var allResources = await resourceManagementClient.Resources.ListByResourceGroupAsync(resourceGroupName);
                regionResources = allResources.Where(resource => resource.Location.Equals(region, StringComparison.OrdinalIgnoreCase));

                await stdout.WriteLineAsync($"Found {allResources.Count()} resources in {azureSubscription.DisplayName}, {regionResources.Count()} in {region}");
                if (Verbose)
                {
                    foreach (var resource in regionResources)
                    {
                        await stdout.WriteLineAsync($"Found resource \"{resource.Name}\" of type {resource.Type} in {azureSubscription.DisplayName}");
                    }
                }
            }
            catch (Exception ex)
            {
                await stderr.WriteLineAsync($"Subscription {azureSubscription.DisplayName} error : {ex.Message}");
            }

            return regionResources;
        }

        private async Task<IEnumerable<GenericResourceInner>> EnumerateVirtualMachineResources(
            IEnumerable<GenericResourceInner> allRegionResources,
            string resourceGroupName,
            IResourceManagementClient resourceManagementClient,
            TextWriter stdout,
            TextWriter stderr)
        {
            var virtualMachineResources = new List<GenericResourceInner>();

            var virtualMachines = allRegionResources.Where(resource => resource.Type == VirtualMachineResourceType);
            if (virtualMachines.Any())
            {
                var resourceProviders = await resourceManagementClient.Providers.ListAsync();

                foreach (var virtualMachine in virtualMachines)
                {
                    virtualMachineResources.Add(virtualMachine);
                    foreach (var resourceType in VirtualMachineResourceTypes)
                    {
                        var resourceName = $"{virtualMachine.Name}{resourceType.Suffix}";
                        var resource = allRegionResources.FirstOrDefault(resource => resource.Name == resourceName);
                        if (resource != null)
                        {
                            virtualMachineResources.Add(resource);
                        }
                        else
                        {
                            // If for some reason an expected vm resource was not enumerated by the resource
                            // manager we check for its existence explicitly.
                            var foundResource = await GetResource(
                                resourceName,
                                resourceType.Type,
                                resourceGroupName,
                                resourceProviders,
                                resourceManagementClient,
                                stderr);

                            if (foundResource != null)
                            {
                                await WriteVerboseLineAsync(stdout, $"WARNING: Found \"{resourceName}\" by explicit resource lookup");
                                virtualMachineResources.Add(foundResource);
                            }
                        }
                    }
                }
            }

            return virtualMachineResources;
        }

        private async Task<GenericResourceInner> GetResource(
            string resourceName,
            string resourceType,
            string resourceGroupName,
            IEnumerable<ProviderInner> resourceProviders,
            IResourceManagementClient resourceManagementClient,
            TextWriter stderr)
        {
            GenericResourceInner foundResource = null;

            var apiVersion = GetLatestResourceApiVersion(resourceProviders, resourceType);
            var resourceTypeComponents = GetResourceTypeComponents(resourceType);
            try
            {
                foundResource = await resourceManagementClient.Resources.GetAsync(
                    resourceGroupName,
                    resourceTypeComponents.Namespace,
                    string.Empty,
                    resourceTypeComponents.Type,
                    resourceName,
                    apiVersion);
            }
            catch (Exception)
            {
                await WriteVerboseLineAsync(stderr, $"No vm resource with name \"{resourceName}\" has been found.");
            }

            return foundResource;
        }

        private async Task DeleteResources(IEnumerable<GenericResourceInner> resources, IResourceManagementClient resourceManagementClient, TextWriter stdout, TextWriter stderr)
        {
            var resourceProviders = await resourceManagementClient.Providers.ListAsync();
            foreach (var resource in resources)
            {
                var apiVersion = GetLatestResourceApiVersion(resourceProviders, resource.Type);
                await TryDeleteResourceAsync(resource, () => resourceManagementClient.Resources.DeleteByIdAsync(resource.Id, apiVersion), stdout, stderr);
            }
        }

        /// <summary>
        /// This method deletes virtual machine based resources in the order that mirrors VirtualMachineDeploymentManager.BeginDeleteComputeAsync,
        /// utilising the appropriate managment client for the resource type.
        /// </summary>
        private async Task DeleteVirtualMachineResources(
            IEnumerable<GenericResourceInner> resources,
            string region,
            IResourceNameBuilder resourceNameBuilder,
            IAzureSubscription azureSubscription,
            TextWriter stdout,
            TextWriter stderr)
        {
            await WriteVerboseLineAsync(stdout, $"Deleting vm specific resources in {azureSubscription.DisplayName}...");

            var resourceGroupName = resourceNameBuilder.GetResourceGroupName(string.Empty);
            var azureClientFactory = GetServiceProvider().GetRequiredService<IAzureClientFactory>();
            var computeManagementClient = await azureClientFactory.GetComputeManagementClient(Guid.Parse(azureSubscription.SubscriptionId));

            var virtualMachines = resources.Where(resource => resource.Type == VirtualMachineResourceType);
            if (virtualMachines.Any())
            {
                foreach (var virtualMachine in virtualMachines)
                {
                    await TryDeleteResourceAsync(
                            virtualMachine,
                            () => computeManagementClient.VirtualMachines.DeleteAsync(resourceGroupName, virtualMachine.Name),
                            stdout,
                            stderr);

                    // TODO: Find a safe way to clean up input-queue
                    // await TryDeleteInputQueue(virtualMachine.Name, region, resourceNameBuilder, stdout, stderr);
                }
            }

            var disks = resources.Where(resource => resource.Type == DiskResourceType);
            if (disks.Any())
            {
                foreach (var disk in disks)
                {
                    await TryDeleteResourceAsync(
                            disk,
                            () => computeManagementClient.Disks.DeleteAsync(resourceGroupName, disk.Name),
                            stdout,
                            stderr);
                }
            }

            var logger = GetServiceProvider().GetRequiredService<IDiagnosticsLogger>();
            var networkManagementClient = await azureClientFactory.GetNetworkManagementClient(Guid.Parse(azureSubscription.SubscriptionId), logger);
            var networkInterfaces = resources.Where(resource => resource.Type == NetworkInterfaceResourceType);
            if (networkInterfaces.Any())
            {
                foreach (var networkInterface in networkInterfaces)
                {
                    await TryDeleteResourceAsync(
                            networkInterface,
                            () => networkManagementClient.NetworkInterfaces.DeleteAsync(resourceGroupName, networkInterface.Name),
                            stdout,
                            stderr);
                }
            }

            var networkSecurityGroups = resources.Where(resource => resource.Type == NetworkSecurityGroupResourceType);
            if (networkSecurityGroups.Any())
            {
                foreach (var networkSecurityGroup in networkSecurityGroups)
                {
                    await TryDeleteResourceAsync(
                            networkSecurityGroup,
                            () => networkManagementClient.NetworkSecurityGroups.DeleteAsync(resourceGroupName, networkSecurityGroup.Name),
                            stdout,
                            stderr);
                }
            }

            var virtualNetworks = resources.Where(resource => resource.Type == VirtualNetworksResourceType);
            if (virtualNetworks.Any())
            {
                foreach (var virtualNetwork in virtualNetworks)
                {
                    await TryDeleteResourceAsync(
                            virtualNetwork,
                            () => networkManagementClient.VirtualNetworks.DeleteAsync(resourceGroupName, virtualNetwork.Name),
                            stdout,
                            stderr);
                }
            }
        }

        private async Task TryDeleteResourceAsync(GenericResourceInner resource, Func<Task> deletionTask, TextWriter stdout, TextWriter stderr)
        {
            try
            {
                await WriteVerboseLineAsync(stdout, $"Deleting resource \"{resource.Name}\" of type {resource.Type}...");
                await DoWithDryRun(() => deletionTask.Invoke());
                await WriteVerboseLineAsync(stdout, $"Resource \"{resource.Name}\" successfully deleted.");
            }
            catch (Exception ex)
            {
                await stderr.WriteLineAsync($"Error encountered while deleting resource \"{resource.Name}\" - {ex.Message}");
            }
        }

        private async Task TryDeleteInputQueue(string name, string region, IResourceNameBuilder resourceNameBuilder, TextWriter stdout, TextWriter stderr)
        {
            var inputQueueName = $"{name.ToLowerInvariant()}-input-queue";
            try
            {
                await WriteVerboseLineAsync(stdout, $"Looking up input queue with name {inputQueueName}...");

                var controlPlaneAzureResourceAccessor = GetControlPlaneAzureResourceAccessorForRegion(region, resourceNameBuilder);
                var logger = GetServiceProvider().GetRequiredService<IDiagnosticsLogger>();
                var storageInfo = await controlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeQueuesAsync(Enum.Parse<AzureLocation>(region), logger);

                var storageCredentials = new StorageCredentials(storageInfo.StorageAccountName, storageInfo.StorageAccountKey);
                var storageAccount = new CloudStorageAccount(storageCredentials, useHttps: true);
                var queueClient = new CloudQueueClient(storageAccount.QueueStorageUri, storageCredentials);
                var queue = queueClient.GetQueueReference(inputQueueName);

                await WriteVerboseLineAsync(stdout, $"Found input queue \"{inputQueueName}\". Attempting to delete...");
                await DoWithDryRun(() => queue.DeleteIfExistsAsync());
                await WriteVerboseLineAsync(stdout, $"Input queue \"{inputQueueName}\" successfully deleted.");
            }
            catch (Exception ex)
            {
                await stderr.WriteLineAsync($"Error encountered while deleting input queue \"{inputQueueName}\" - {ex.Message}");
            }
        }

        /// <summary>
        /// This method creates a region specific ControlPlaneAzureResourceAccessor by reconstructing the necessary underlying settings objects
        ///  using the appropriate region settings.
        /// </summary>
        private IControlPlaneAzureResourceAccessor GetControlPlaneAzureResourceAccessorForRegion(string region, IResourceNameBuilder resourceNameBuilder)
        {
            // We grab the ControlPlaneSettings from the existing ControlPlaneInfo via reflection as it doesnt make sense to expose it publicly on that object.
            // TODO: Find a less fragile way to get this state.
            var currentControlPlaneInfo = GetServiceProvider().GetRequiredService<IControlPlaneInfo>() as ControlPlaneInfo;
            var controlPlaneSettingsProperty = currentControlPlaneInfo.GetType().GetProperty("ControlPlaneSettings", BindingFlags.Instance | BindingFlags.NonPublic);
            var controlPlaneInfoOptions = Options.Create(new ControlPlaneInfoOptions()
            {
                ControlPlaneSettings = controlPlaneSettingsProperty.GetValue(currentControlPlaneInfo) as ControlPlaneSettings,
            });

            var controlPlaneInfo = new ControlPlaneInfo(
                controlPlaneInfoOptions,
                new CurrentLocationProvider(Enum.Parse<AzureLocation>(region)),
                resourceNameBuilder);

            return new ControlPlaneAzureResourceAccessor(
                controlPlaneInfo,
                GetServiceProvider().GetRequiredService<IServicePrincipal>(),
                GetServiceProvider().GetRequiredService<ControlPlaneAzureResourceAccessor.HttpClientWrapper>(),
                GetServiceProvider().GetRequiredService<IManagedCache>());
        }

        private Task DeleteDatabaseCollectionInRegion(
            string region,
            IResourceNameBuilder resourceNameBuilder,
            TextWriter stdout,
            TextWriter stderr)
        {
            return Task.Run(async () =>
            {
                var dbName = resourceNameBuilder.GetCosmosDocDBName(CloudEnvironmentDbName);
                var controlPlaneAzureResourceAccessor = GetControlPlaneAzureResourceAccessorForRegion(region, resourceNameBuilder);
                var (hostUrl, authKey) = await controlPlaneAzureResourceAccessor.GetStampCosmosDbAccountAsync();
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
