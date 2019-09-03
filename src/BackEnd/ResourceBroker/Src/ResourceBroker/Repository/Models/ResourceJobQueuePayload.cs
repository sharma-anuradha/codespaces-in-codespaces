// <copyright file="ResourceJobQueuePayload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Core payload that is used to queue the continuation request.
    /// </summary>
    public class ResourceJobQueuePayload
    {
        /// <summary>
        /// Gets or sets the name that the job targeting.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets the Id that can be used for tracking.
        /// </summary>
        public string TrackingId { get; set; }

        /// <summary>
        /// Gets or sets the time the original operation was triggered.
        /// </summary>
        public DateTime Created { get; set; }

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
        /// Converts payload to JSON.
        /// </summary>
        /// <returns>JSON string.</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(
                this, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
        }
    }
}