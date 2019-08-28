// <copyright file="DeploymentStatusInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    /// <summary>
    /// Input for next continuation.
    /// </summary>
    public class NextStageInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NextStageInput"/> class.
        /// </summary>
        /// <param name="trackingId">Tracking id to resume next phase.</param>
        /// <param name="azureResourceInfo">The azure resource info.</param>
        [JsonConstructor]
        public NextStageInput(string trackingId, AzureResourceInfo azureResourceInfo)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            TrackingId = trackingId;
            AzureResourceInfo = azureResourceInfo;
        }

        /// <summary>
        /// Gets the tracking id for next continuation phase.
        /// </summary>
        public string TrackingId { get; }

        /// <summary>
        /// Gets the compute azure resource info.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; }
    }
}