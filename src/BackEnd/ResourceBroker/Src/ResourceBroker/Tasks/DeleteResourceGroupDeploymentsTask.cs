// <copyright file="DeleteResourceGroupDeploymentsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Rest.Azure;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Deletes deployment history on Resource Groups in the data plane subscriptions
    /// to avoid hitting the 800 deployment history limit on Azure Resource Groups.
    /// </summary>
    public class DeleteResourceGroupDeploymentsTask : BaseDataPlaneResourceGroupTask, IDeleteResourceGroupDeploymentsTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteResourceGroupDeploymentsTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="capacityManager">Target capacity manager.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="azureClientFactory">Azure client factory.</param>
        public DeleteResourceGroupDeploymentsTask(
            ResourceBrokerSettings resourceBrokerSettings,
            ITaskHelper taskHelper,
            ICapacityManager capacityManager,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IAzureClientFactory azureClientFactory)
            : base(
                  resourceBrokerSettings,
                  taskHelper,
                  capacityManager,
                  claimedDistributedLease,
                  resourceNameBuilder)
        {
            AzureClientFactory = azureClientFactory;
        }

        /// <inheritdoc/>
        protected override string TaskName { get; } = nameof(DeleteResourceGroupDeploymentsTask);

        /// <inheritdoc/>
        protected override string LogBaseName { get; } = ResourceLoggingConstants.DeleteResourceGroupDeploymentsTask;

        private IAzureClientFactory AzureClientFactory { get; }

        /// <inheritdoc/>
        protected override async Task ProcessResourceGroupAsync(IAzureResourceGroup resourceGroup, IDiagnosticsLogger logger)
        {
            var count = 0;
            var page = 0;
            var records = default(IPage<DeploymentExtendedInner>);

            do
            {
                await logger.OperationScopeAsync(
                    $"{LogBaseName}_run_resourcegroup_check_page",
                    async (childLogger) =>
                    {
                        var azure = await AzureClientFactory.GetResourceManagementClient(Guid.Parse(resourceGroup.Subscription.SubscriptionId));

                        // Get deployments one page at a time
                        records = records == null ?
                            await azure.Deployments.ListByResourceGroupAsync(resourceGroup.ResourceGroup)
                            : await azure.Deployments.ListByResourceGroupNextAsync(records.NextPageLink);

                        childLogger.FluentAddValue("TaskFoundItems", count += records.Count())
                            .FluentAddValue("TaskFoundPage", page++);

                        // Find the completed deployments
                        // https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-manager-async-operations#provisioningstate-values
                        var deployments = records.Where(x =>
                            x.Properties?.ProvisioningState == "Succeeded" ||
                            x.Properties?.ProvisioningState == "Failed" ||
                            x.Properties?.ProvisioningState == "Canceled");

                        foreach (var deployment in deployments)
                        {
                            await DeleteDeploymentAsync(azure, deployment, resourceGroup, childLogger);
                        }
                    });
            }
            while (!string.IsNullOrEmpty(records?.NextPageLink));
        }

        private Task DeleteDeploymentAsync(IResourceManagementClient azure, DeploymentExtendedInner deployment, IAzureResourceGroup resourceGroup, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_deployment_delete",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("DeploymentId", deployment.Id)
                        .FluentAddBaseValue("DeploymentName", deployment.Name);

                    await azure.Deployments.BeginDeleteAsync(resourceGroup.ResourceGroup, deployment.Name);
                },
                swallowException: true); // Allow other deployment deletions to continue
        }
    }
}
