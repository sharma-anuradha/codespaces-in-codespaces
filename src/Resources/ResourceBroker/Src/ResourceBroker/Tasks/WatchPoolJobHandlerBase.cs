// <copyright file="WatchPoolJobHandlerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Base class for all our watch pool job handlers.
    /// </summary>
    /// <typeparam name="TJobHandlerType">Type of the job handler type.</typeparam>
    public abstract class WatchPoolJobHandlerBase<TJobHandlerType> : JobHandlerPayloadBase<WatchPoolPayloadFactory.ResourcePoolPayload<TJobHandlerType>>
        where TJobHandlerType : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchPoolJobHandlerBase{T}"/> class.
        /// </summary>
        /// <param name="resourcePoolDefinitionStore">Resource pool definition store.</param>
        /// <param name="taskHelper">Task helper.</param>
        public WatchPoolJobHandlerBase(
           IResourcePoolDefinitionStore resourcePoolDefinitionStore,
           ITaskHelper taskHelper)
           : base(options: JobHandlerOptions.WithValues(1))
        {
            ResourcePoolDefinitionStore = resourcePoolDefinitionStore;
            TaskHelper = taskHelper;
        }

        /// <summary>
        /// gets the Task helper instance.
        /// </summary>
        protected ITaskHelper TaskHelper { get; }

        /// <summary>
        /// Gets the base logging name.
        /// </summary>
        protected abstract string LogBaseName { get; }

        private IResourcePoolDefinitionStore ResourcePoolDefinitionStore { get; }

        /// <inheritdoc/>
        protected override async Task HandleJobAsync(WatchPoolPayloadFactory.ResourcePoolPayload<TJobHandlerType> payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(payload.PoolId))
            {
                throw new NotSupportedException("Pool id == null");
            }

            var resources = await ResourcePoolDefinitionStore.RetrieveDefinitionsAsync();
            var resourcePool = resources.FirstOrDefault(r => r.Id == payload.PoolId);
            if (resourcePool == null)
            {
                throw new Exception($"Id:{payload.PoolId} not found on resource pool store");
            }

            await logger.OperationScopeAsync(
                $"{LogBaseName}_run_unit_check",
                (childLogger) => HandleJobAsync(resourcePool, childLogger, cancellationToken));
        }

        /// <summary>
        /// Process the resource pool instance.
        /// </summary>
        /// <param name="resourcePool">Resource pool instance.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        protected abstract Task HandleJobAsync(ResourcePool resourcePool, IDiagnosticsLogger logger, CancellationToken cancellationToken);
    }
}
