// <copyright file="DeleteResourceGroupDeploymentsJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Rest.Azure;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Deletes deployment history on Resource Groups in the data plane subscriptions
    /// to avoid hitting the 800 deployment history limit on Azure Resource Groups.
    /// </summary>
    public class DeleteResourceGroupDeploymentsJobHandler : BaseDataPlaneResourceGroupJobHandler<DeleteResourceGroupDeploymentsJobHandler>
    {
        private const int DeleteDelayInMin = 30;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteResourceGroupDeploymentsJobHandler"/> class.
        /// </summary>
        /// <param name="azureClientFactory">Azure client factory.</param>
        /// <param name="jobQueueProducerFactory">Job queue producer factory.</param>
        public DeleteResourceGroupDeploymentsJobHandler(
            IAzureClientFactory azureClientFactory,
            IJobQueueProducerFactory jobQueueProducerFactory)
        {
            AzureClientFactory = azureClientFactory;
            JobQueueProducer = jobQueueProducerFactory.GetOrCreate(ResourceJobQueueConstants.GenericQueueName);
        }

        private string LogBaseName { get; } = ResourceLoggingConstants.DeleteResourceGroupDeploymentsJobHandler;

        private IAzureClientFactory AzureClientFactory { get; }

        private IJobQueueProducer JobQueueProducer { get; }

        /// <inheritdoc/>
        protected override async Task HandleJobAsync(string subscriptionId, string resourceGroupName, IDiagnosticsLogger logger, CancellationToken cancellationToken)
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
                        var azure = await AzureClientFactory.GetResourceManagementClient(Guid.Parse(subscriptionId));

                        // Get deployments one page at a time
                        records = records == null ?
                            await azure.Deployments.ListByResourceGroupAsync(resourceGroupName)
                            : await azure.Deployments.ListByResourceGroupNextAsync(records.NextPageLink);

                        childLogger.FluentAddValue("TaskFoundItems", count += records.Count())
                            .FluentAddValue("TaskFoundPage", page++);

                        // Find the completed deployments
                        // https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-manager-async-operations#provisioningstate-values
                        var deployments = records.Where(x =>
                            (x.Properties?.ProvisioningState == "Succeeded" ||
                            x.Properties?.ProvisioningState == "Failed" ||
                            x.Properties?.ProvisioningState == "Canceled")
                            &&
                            (!x.Name.StartsWith("Create", StringComparison.OrdinalIgnoreCase) ||
                            x.Properties?.Timestamp < (DateTime.UtcNow - TimeSpan.FromMinutes(DeleteDelayInMin))));

                        foreach (var deployment in deployments)
                        {
                            var jobPayload = new DeleteOneDeploymentPayload()
                            {
                                SubscriptionId = subscriptionId,
                                ResourceGroupName = resourceGroupName,
                                DeploymentId = deployment.Id,
                                DeploymentName = deployment.Name,
                            };
                            var jobPayloadOptions = new JobPayloadOptions()
                            {
                                ExpireTimeout = JobPayloadOptions.DefaultJobPayloadExpireTimeout,
                            };

                            await JobQueueProducer.AddJobAsync(jobPayload, jobPayloadOptions, childLogger, default);
                        }
                    });
            }
            while (!string.IsNullOrEmpty(records?.NextPageLink));
        }
    }
}
