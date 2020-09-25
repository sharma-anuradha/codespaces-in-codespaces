// <copyright file="IGuidShardJobScheduleDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Producers
{
    /// <summary>
    /// Interface for job handlers which consume job payloads produced by <see cref="GuidShardJobProducer"/>.
    /// </summary>    
    public interface IGuidShardJobScheduleDetails
    {
        /// <summary>
        /// The enablement feature flag name
        /// </summary>
        string EnabledFeatureFlagName { get; }

        /// <summary>
        /// The job name
        /// </summary>
        string JobName { get; }

        /// <summary>
        /// The target queue id
        /// </summary>
        string QueueId { get; }

        /// <summary>
        /// The payload and lease schedule
        /// </summary>
        (string CronExpression, TimeSpan Interval) ScheduleTimeInterval { get; }

        /// <summary>
        /// Payload tag type. The job handler should consume payloads of type GuidShardPayload<{PayloadTagType}>
        /// </summary>
        Type PayloadTagType { get; }
    }
}
