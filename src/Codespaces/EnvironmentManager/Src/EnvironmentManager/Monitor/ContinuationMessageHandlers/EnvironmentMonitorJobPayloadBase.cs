// <copyright file="EnvironmentMonitorJobPayloadBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers.Models
{
    /// <summary>
    /// Base class for Environment Monitor Input.
    /// </summary>
    public class EnvironmentMonitorJobPayloadBase : JobPayload
    {
        /// <summary>
        /// Gets or sets the reference id.
        /// </summary>
        public string EnvironmentId { get; set; }

        /// <summary>
        /// Gets or sets the compute resource id.
        /// </summary>
        public Guid ComputeResourceId { get; set; }
    }
}