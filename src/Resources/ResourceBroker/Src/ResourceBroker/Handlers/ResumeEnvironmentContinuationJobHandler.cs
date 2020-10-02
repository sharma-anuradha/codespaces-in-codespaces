// <copyright file="ResumeEnvironmentContinuationJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;
using QueueMessage = Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts.QueueMessage;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
#pragma warning disable CA1501
    /// <summary>
    /// Continuation handler that manages starting of environment.
    /// </summary>
    public class ResumeEnvironmentContinuationJobHandler
        : StartEnvironmentContinuationJobHandlerBase<ResumeEnvironmentContinuationJobHandler.Payload, ResumeEnvironmentContinuationJobHandler.ResumeEnvironmentContinuationResult>
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueId = "jobhandler-resume-environment";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResumeEnvironmentContinuationHandler"/> class.
        /// </summary>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="storageFileShareProviderHelper">Storage File Share Provider Helper.</param>
        /// <param name="queueProvider">Queue provider.</param>
        /// <param name="resourceStateManager">Resource state manager.</param>
        /// <param name="jobQueueProducerFactory">A job queue producer factory.</param>
        public ResumeEnvironmentContinuationJobHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository,
            IStorageFileShareProviderHelper storageFileShareProviderHelper,
            IQueueProvider queueProvider,
            IResourceStateManager resourceStateManager,
            IJobQueueProducerFactory jobQueueProducerFactory)
            : base(computeProvider, storageProvider, resourceRepository, storageFileShareProviderHelper, queueProvider, resourceStateManager, jobQueueProducerFactory)
        {
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerStartEnvironment;

        /// <inheritdoc/>
        public override string QueueId => DefaultQueueId;

        /// <inheritdoc/>
        protected override VirtualMachineProviderStartComputeInput CreateStartComputeInput(Payload input, IEntityRecordRef<ResourceRecord> compute, ShareConnectionInfo shareConnectionInfo, ComputeOS computeOs, AzureLocation azureLocation)
        {
            return new VirtualMachineProviderStartComputeInput(
                compute.Value.AzureResourceInfo,
                shareConnectionInfo,
                input.EnvironmentVariables,
                input.UserSecrets,
                input.DevContainer,
                computeOs,
                azureLocation,
                compute.Value.SkuName,
                null);
        }

        /// <inheritdoc/>
        protected override QueueMessage GeneratePayload(VirtualMachineProviderStartComputeInput startComputeInput)
        {
            return startComputeInput.GenerateStartEnvironmentPayload();
        }

        [JobPayload(JobPayloadNameOption.Name)]
        public class Payload : StartEnvironmentContinuationPayloadBase
        {
            /// <summary>
            /// Gets or sets the devcontainer JSON.
            /// </summary>
            public string DevContainer { get; set; }
        }

        public class ResumeEnvironmentContinuationResult : EntityContinuationResult
        {
        }
    }
#pragma warning restore CA1501
}
