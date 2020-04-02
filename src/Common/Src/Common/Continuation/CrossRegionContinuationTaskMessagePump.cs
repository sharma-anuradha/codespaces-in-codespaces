// <copyright file="CrossRegionContinuationTaskMessagePump.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Message pump which push messages to the underlying queue for a give control plane region.
    /// </summary>
    public class CrossRegionContinuationTaskMessagePump : ICrossRegionContinuationTaskMessagePump
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CrossRegionContinuationTaskMessagePump"/> class.
        /// </summary>
        /// <param name="crossRegionContinuationJobQueueRepository">Underlying resourcec job queue repository for cross region communication.</param>
        public CrossRegionContinuationTaskMessagePump(
            ICrossRegionContinuationJobQueueRepository crossRegionContinuationJobQueueRepository)
        {
            CrossRegionContinuationJobQueueRepository = crossRegionContinuationJobQueueRepository;
        }

        private ICrossRegionContinuationJobQueueRepository CrossRegionContinuationJobQueueRepository { get; }

        /// <inheritdoc/>
        public async Task PushMessageToControlPlaneRegionAsync(ContinuationQueuePayload payload, AzureLocation controlPlaneRegion, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger)
        {
            await CrossRegionContinuationJobQueueRepository.AddAsync(payload.ToJson(), controlPlaneRegion, initialVisibilityDelay, logger);
        }
    }
}
