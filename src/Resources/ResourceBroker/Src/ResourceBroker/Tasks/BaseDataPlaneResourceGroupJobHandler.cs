// <copyright file="BaseDataPlaneResourceGroupJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Base class for all data plane resource group job handlers
    /// </summary>
    /// <typeparam name="TJobHandlerType">Type of the job handler type.</typeparam>
    public abstract class BaseDataPlaneResourceGroupJobHandler<TJobHandlerType> : JobHandlerPayloadBase<BaseDataPlaneResourceGroupJobProducer.ResourceGroupPayload<TJobHandlerType>>
       where TJobHandlerType : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataPlaneResourceGroupJobHandler{T}"/> class.
        /// </summary>
        public BaseDataPlaneResourceGroupJobHandler()
           : base(options: JobHandlerOptions.WithValues(1))
        {
        }

        /// <inheritdoc/>
        protected override async Task HandleJobAsync(BaseDataPlaneResourceGroupJobProducer.ResourceGroupPayload<TJobHandlerType> payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(payload.SubscriptionId) || string.IsNullOrEmpty(payload.ResourceGroupName))
            {
                throw new NotSupportedException($"Invalid payload. Subsciption id: {payload.SubscriptionId}, ResourceGroupName: {payload.ResourceGroupName}");
            }

            await logger.OperationScopeAsync(
                "data_plane_resource_group_job_handler",
                (childLogger) => HandleJobAsync(payload.SubscriptionId, payload.ResourceGroupName, childLogger, cancellationToken));
        }

        /// <summary>
        /// Process the resource group instance.
        /// </summary>
        /// <param name="subscriptionId">Azure subscription Id.</param>
        /// <param name="resourceGroupName">Resource group name.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        protected abstract Task HandleJobAsync(string subscriptionId, string resourceGroupName, IDiagnosticsLogger logger, CancellationToken cancellationToken);
    }
}
