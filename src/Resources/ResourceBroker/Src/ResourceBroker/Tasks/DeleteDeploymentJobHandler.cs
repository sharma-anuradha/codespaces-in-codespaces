// <copyright file="DeleteDeploymentJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Perform the work of deleting one deploement history
    /// </summary>
    public class DeleteDeploymentJobHandler : JobHandlerPayloadBase<DeleteDeploymentPayload>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteDeploymentJobHandler"/> class.
        /// </summary>
        /// <param name="azureClientFactory">Azure client factory</param>
        public DeleteDeploymentJobHandler(
            IAzureClientFactory azureClientFactory)
        {
            AzureClientFactory = azureClientFactory;
        }

        private string LogBaseName { get; } = ResourceLoggingConstants.DeleteDeploymentJobHandler;

        private IAzureClientFactory AzureClientFactory { get; }

        /// <inheritdoc/>
        protected override Task HandleJobAsync(DeleteDeploymentPayload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_deployment_delete",
                async (childLogger) =>
                {
                    var azure = await AzureClientFactory.GetResourceManagementClient(Guid.Parse(payload.SubscriptionId));

                    childLogger.FluentAddBaseValue("DeploymentId", payload.DeploymentId)
                    .FluentAddBaseValue("DeploymentName", payload.DeploymentName);

                    await azure.Deployments.BeginDeleteAsync(payload.ResourceGroupName, payload.DeploymentName);
                },
                swallowException: true);
        }
    }
}
