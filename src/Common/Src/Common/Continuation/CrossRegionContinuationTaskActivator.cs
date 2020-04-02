// <copyright file="CrossRegionContinuationTaskActivator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Cross Region Continuation Activator which can trigger a task execution in a given control plane.
    /// </summary>
    public class CrossRegionContinuationTaskActivator : ICrossRegionContinuationTaskActivator
    {
        private const string LogBaseName = "cross_region_continuation_task_activator";

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossRegionContinuationTaskActivator"/> class.
        /// </summary>
        /// <param name="crossRegionMessagePump">Message pump that can be used to put next
        /// messages onto the queue for another control plane region.</param>
        /// <param name="controlPlaneInfo">Control plane info.</param>
        /// <param name="currentLocationProvider">Current location provider.</param>
        /// <param name="continuationTaskActivator">Regular continuation task activator.</param>
        public CrossRegionContinuationTaskActivator(
            ICrossRegionContinuationTaskMessagePump crossRegionMessagePump,
            IControlPlaneInfo controlPlaneInfo,
            ICurrentLocationProvider currentLocationProvider,
            IContinuationTaskActivator continuationTaskActivator)
        {
            CrossRegionMessagePump = crossRegionMessagePump;
            ControlPlaneInfo = controlPlaneInfo;
            CurrentLocationProvider = currentLocationProvider;
            ContinuationTaskActivator = continuationTaskActivator;
        }

        private ICrossRegionContinuationTaskMessagePump CrossRegionMessagePump { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private ICurrentLocationProvider CurrentLocationProvider { get; }

        private IContinuationTaskActivator ContinuationTaskActivator { get; }

        /// <inheritdoc/>
        public Task<ContinuationResult> ExecuteForDataPlane(
            string name,
            AzureLocation dataPlaneRegion,
            ContinuationInput input,
            IDiagnosticsLogger logger,
            Guid? systemId = null,
            IDictionary<string, string> loggerProperties = null)
        {
            var controlPlaneRegion = ControlPlaneInfo.GetOwningControlPlaneStamp(dataPlaneRegion).Location;
            if (CurrentLocationProvider.CurrentLocation == controlPlaneRegion)
            {
                return ContinuationTaskActivator.Execute(name, input, logger, systemId, loggerProperties);
            }

            var payload = ConstructPayload(name, input, logger, ref systemId, loggerProperties);

            return logger.OperationScopeAsync(
                LogBaseName,
                async (childLogger) =>
                {
                    await CrossRegionMessagePump.PushMessageToControlPlaneRegionAsync(payload, controlPlaneRegion, TimeSpan.Zero, logger.WithValues(new LogValueSet()));
                    return new ContinuationResult
                    {
                        Status = OperationState.Triggered,
                    };
                });
        }

        private static ContinuationQueuePayload ConstructPayload(string name, ContinuationInput input, IDiagnosticsLogger logger, ref Guid? trackingId, IDictionary<string, string> loggerProperties)
        {
            var trackingInstanceId = Guid.NewGuid();
            trackingId = trackingId ?? trackingInstanceId;

            var payload = new ContinuationQueuePayload
            {
                TrackingId = trackingId.ToString(),
                TrackingInstanceId = trackingInstanceId.ToString(),
                Created = DateTime.UtcNow,
                Status = null,
                Input = input,
                Target = name,
                LoggerProperties = loggerProperties,
            };

            logger.FluentAddBaseValues(payload.LoggerProperties);
            return payload;
        }
    }
}
