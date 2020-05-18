// <copyright file="CleanResourceGroups.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil
{
    /// <summary>
    /// Cleans up unneeded resource groups.
    /// </summary>
    [Verb("cleanresourcegroups", HelpText = "clean up azure resource groups.")]
    public class CleanResourceGroups : CommandBase
    {
        /// <summary>
        /// Gets or sets the target subscription Id.
        /// </summary>
        [Option('s', "subscription", HelpText = "Target Subscription", Required = true)]
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the resource group name pattern.
        /// </summary>
        [Option('p', "pattern", Default = null, HelpText = "Pattern of RG names to consider")]
        public string NamePattern { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether only empty resource groups should be considered.
        /// </summary>
        [Option("only-empty", Default = true, HelpText = "Consider only empty RGs")]
        public bool OnlyEmpty { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to continue on deleting on error.
        /// </summary>
        [Option("continue-on-error", Default = false, HelpText = "Continue deleting other RGs if an error occurs while deleting one")]
        public bool ContinueOnError { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            ExecuteAsync(services, stdout, stderr).Wait();
        }

        private async Task ExecuteAsync(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            var azureClientFactory = GetServiceProvider().GetRequiredService<IAzureClientFactory>();
            var azure = await azureClientFactory.GetResourceManagementClient(Guid.Parse(SubscriptionId));

            var resourceGroups = await GetResourceGroupsToDeleteAsync(azure, stdout, stderr);

            await DeleteResourceGroupsAsync(azure, resourceGroups, stdout, stderr);
        }

        private async Task<List<string>> GetResourceGroupsToDeleteAsync(IResourceManagementClient azure, TextWriter stdout, TextWriter stderr)
        {
            var resourceGroupsToDelete = new List<string>();
            var totalCount = 0;

            foreach (var resourceGroup in await azure.ResourceGroups.ListAsync())
            {
                ++totalCount;

                if (!string.IsNullOrEmpty(NamePattern) && !Regex.IsMatch(resourceGroup.Name, NamePattern))
                {
                    await WriteVerboseLineAsync(stdout, $"resource group {resourceGroup.Name} not considered as it does not match the pattern.");
                    continue;
                }

                if (OnlyEmpty && (await azure.Resources.ListByResourceGroupAsync(resourceGroup.Name)).Any())
                {
                    await WriteVerboseLineAsync(stdout, $"resource group {resourceGroup.Name} not considered as it is not empty.");
                    continue;
                }

                await stdout.WriteLineAsync($"resource group {resourceGroup.Name} will be deleted.");

                resourceGroupsToDelete.Add(resourceGroup.Name);
            }

            await stdout.WriteLineAsync($"found {resourceGroupsToDelete.Count} total resource groups to delete from a total of {totalCount}.");

            return resourceGroupsToDelete;
        }

        private async Task DeleteResourceGroupsAsync(IResourceManagementClient azure, IEnumerable<string> resourceGroups, TextWriter stdout, TextWriter stderr)
        {
            foreach (var resourceGroup in resourceGroups)
            {
                try
                {
                    await stdout.WriteLineAsync($"deleting resource group {resourceGroup}...");

                    await DoWithDryRun(() => azure.ResourceGroups.DeleteAsync(resourceGroup));

                    await stdout.WriteLineAsync($"...done");
                }
                catch (Exception e)
                {
                    await stderr.WriteLineAsync($"encountered exception while deleting resource group {resourceGroup}: ");
                    await stderr.WriteLineAsync(e.ToString());

                    if (!ContinueOnError)
                    {
                        return;
                    }
                }
            }
        }
    }
}
