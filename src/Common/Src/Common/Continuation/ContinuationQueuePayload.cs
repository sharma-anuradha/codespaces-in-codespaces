// <copyright file="ContinuationQueuePayload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Core payload that is used to queue the continuation request.
    /// </summary>
    public class ContinuationQueuePayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationQueuePayload"/> class.
        /// </summary>
        public ContinuationQueuePayload()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationQueuePayload"/> class.
        /// </summary>
        /// <param name="trackingId">Tracking id for this job.</param>
        /// <param name="trackingInstanceId">Tracking instance id for this job.</param>
        /// <param name="target">Target handler.</param>
        /// <param name="created">Created date/time.</param>
        /// <param name="stepCount">What step count we are at.</param>
        /// <param name="loggerProperties">Target logger properties.</param>
        public ContinuationQueuePayload(
            string trackingId, string trackingInstanceId, string target, DateTime created, int stepCount, IDictionary<string, string> loggerProperties)
        {
            TrackingId = trackingId;
            TrackingInstanceId = trackingInstanceId;
            Created = created;
            Target = target;
            StepCount = stepCount;
            LoggerProperties = loggerProperties;
        }

        /// <summary>
        /// Gets or sets the name that the job targeting.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets the Id that can be used for tracking.
        /// </summary>
        public string TrackingId { get; set; }

        /// <summary>
        /// Gets or sets the Id that can be used for tracking this instance.
        /// </summary>
        public string TrackingInstanceId { get; set; }

        /// <summary>
        /// Gets or sets the time the original operation was triggered.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the current stepCount.
        /// </summary>
        public int StepCount { get; set; }

        /// <summary>
        /// Gets or sets the input thats going to be provided.
        /// </summary>
        public ContinuationInput Input { get; set; }

        /// <summary>
        /// Gets or sets the status of the last result.
        /// </summary>
        public OperationState? Status { get; set; }

        /// <summary>
        /// Gets or sets the next retry after period.
        /// </summary>
        public TimeSpan? RetryAfter { get; set; }

        /// <summary>
        /// Gets or sets the logger properties that should be flowed to each continuation.
        /// </summary>
        public IDictionary<string, string> LoggerProperties { get; set; }

        /// <summary>
        /// Converts payload to JSON.
        /// </summary>
        /// <returns>JSON string.</returns>
        public string ToJson()
        {
#pragma warning disable CA2326 // Do not use TypeNameHandling values other than None
            return JsonConvert.SerializeObject(
                this, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
#pragma warning restore CA2326 // Do not use TypeNameHandling values other than None
        }
    }
}