// <copyright file="DeleteOrphanedResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Delete Orphaned Resource Handler.
    /// </summary>
    public class DeleteOrphanedResourceContinuationHandler : IDeleteOrphanedResourceContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "JobDeleteOrphanedResource";

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteOrphanedResourceContinuationHandler"/> class.
        /// </summary>
        /// <param name="computeProvider">Target compute provider.</param>
        /// <param name="storageProvider">Target storage provider.</param>
        public DeleteOrphanedResourceContinuationHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider)
        {
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
        }

        private IComputeProvider ComputeProvider { get; set; }

        private IStorageProvider StorageProvider { get; set; }

        private string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        public bool CanHandle(ResourceJobQueuePayload payload)
        {
            return payload.Target == DefaultTarget;
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> Continue(ContinuationInput input, IDiagnosticsLogger logger)
        {
            var result = default(ContinuationResult);

            if (input is VirtualMachineProviderDeleteInput computeInput)
            {
                result = await ComputeProvider.DeleteAsync(computeInput, logger.WithValues(new LogValueSet()));
            }
            else if (input is FileShareProviderDeleteInput storageInput)
            {
                result = await StorageProvider.DeleteAsync(storageInput, logger.WithValues(new LogValueSet()));
            }
            else
            {
                throw new NotSupportedException($"Continuation type is not supported - {input.GetType().Name}");
            }

            return result;
        }
    }
}
