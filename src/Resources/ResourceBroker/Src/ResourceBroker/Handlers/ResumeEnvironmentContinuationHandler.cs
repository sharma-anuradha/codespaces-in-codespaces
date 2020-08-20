// <copyright file="ResumeEnvironmentContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation handler that manages starting of environment.
    /// </summary>
    public class ResumeEnvironmentContinuationHandler
        : BaseStartEnvironmentContinuationHandler<StartEnvironmentContinuationInput>, IStartEnvironmentContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "JobStartEnvironment";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResumeEnvironmentContinuationHandler"/> class.
        /// </summary>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="serviceProvider">Service Provider.</param>
        /// <param name="storageFileShareProviderHelper">Storage File Share Provider Helper.</param>
        /// <param name="queueProvider">Queue provider.</param>
        /// <param name="resourceStateManager">Resource state manager.</param>
        public ResumeEnvironmentContinuationHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository,
            IServiceProvider serviceProvider,
            IStorageFileShareProviderHelper storageFileShareProviderHelper,
            IQueueProvider queueProvider,
            IResourceStateManager resourceStateManager)
            : base(computeProvider, storageProvider, resourceRepository, serviceProvider, storageFileShareProviderHelper, queueProvider, resourceStateManager)
        {
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerStartEnvironment;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override Task<ContinuationInput> BuildOperationInputAsync(StartEnvironmentContinuationInput input, ResourceRecordRef compute, IDiagnosticsLogger logger)
        {
            return ConfigureBuildOperationInputAsync(input, compute, logger);
        }

        /// <inheritdoc/>
        protected override VirtualMachineProviderStartComputeInput CreateStartComputeInput(StartEnvironmentContinuationInput input, ResourceRecordRef compute, ShareConnectionInfo shareConnectionInfo, ComputeOS computeOs, AzureLocation azureLocation)
        {
            return new VirtualMachineProviderStartComputeInput(
                compute.Value.AzureResourceInfo,
                shareConnectionInfo,
                input.EnvironmentVariables,
                input.UserSecrets,
                computeOs,
                azureLocation,
                compute.Value.SkuName,
                null);
        }

        /// <inheritdoc/>
        protected override Task<ContinuationResult> RunOperationCoreAsync(StartEnvironmentContinuationInput input, ResourceRecordRef compute, IDiagnosticsLogger logger)
        {
            return ConfigureRunOperationCoreAsync(input, compute, logger);
        }

        /// <inheritdoc/>
        protected override QueueMessage GeneratePayload(VirtualMachineProviderStartComputeInput startComputeInput)
        {
            return startComputeInput.GenerateStartEnvironmentPayload();
        }
    }
}
