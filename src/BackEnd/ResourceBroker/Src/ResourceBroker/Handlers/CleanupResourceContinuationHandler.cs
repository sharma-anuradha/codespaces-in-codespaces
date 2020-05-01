// <copyright file="CleanupResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation handler that manages starting of environment.
    /// </summary>
    public class CleanupResourceContinuationHandler
        : BaseContinuationTaskMessageHandler<CleanupResourceContinuationInput>, ICleanupResourceContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "JobCleanupResource";

        /// <summary>
        /// Initializes a new instance of the <see cref="CleanupResourceContinuationHandler"/> class.
        /// </summary>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="serviceProvider">Service provider.</param>
        public CleanupResourceContinuationHandler(
            IComputeProvider computeProvider,
            IResourceRepository resourceRepository,
            IServiceProvider serviceProvider)
            : base(serviceProvider, resourceRepository)
        {
            ComputeProvider = computeProvider;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerCleanup;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.CleanUp;

        private IComputeProvider ComputeProvider { get; set; }

        private IDiskProvider DiskProvider { get; set; }

        /// <inheritdoc/>
        protected override Task<ContinuationInput> BuildOperationInputAsync(
            CleanupResourceContinuationInput input,
            ResourceRecordRef resource,
            IDiagnosticsLogger logger)
        {
            // Handle compute case only, don't need to do anything with storage here as its already circuited.
            if (resource.Value.Type == ResourceType.ComputeVM)
            {
                var didParseLocation = Enum.TryParse(resource.Value.Location, true, out AzureLocation azureLocation);
                if (!didParseLocation)
                {
                    throw new NotSupportedException($"Provided location of '{resource.Value.Location}' is not supported.");
                }

                var keepDisk = resource.Value.GetComputeDetails().OSDiskRecordId != default;

                return Task.FromResult<ContinuationInput>(
                    new VirtualMachineProviderShutdownInput
                    {
                        AzureResourceInfo = resource.Value.AzureResourceInfo,
                        AzureVmLocation = azureLocation,
                        ComputeOS = resource.Value.PoolReference.GetComputeOS(),
                        EnvironmentId = input.EnvironmentId.ToString(),
                        PreserveOSDisk = keepDisk,
                    });
            }

            throw new NotSupportedException($"Resource type is not supported - {resource.Value.Type}");
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationCoreAsync(CleanupResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            // Handle compute case only, don't need to do anything with storage here as its already circuited.
            if (resource.Value.Type == ResourceType.ComputeVM)
            {
                return await ComputeProvider.ShutdownAsync((VirtualMachineProviderShutdownInput)input.OperationInput, logger.NewChildLogger());
            }

            throw new NotSupportedException($"Resource type is not supported - {resource.Value.Type}");
        }
    }
}
