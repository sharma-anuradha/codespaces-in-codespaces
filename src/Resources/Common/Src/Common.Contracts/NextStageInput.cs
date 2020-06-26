// <copyright file="NextStageInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Input for next continuation.
    /// </summary>
    public class NextStageInput
    {
        /// <summary>
        /// Current version for tracking id.
        /// </summary>
        public static readonly int CurrentVersion = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="NextStageInput"/> class.
        /// </summary>
        [JsonConstructor]
        public NextStageInput()
        {
            RetryAttempt = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NextStageInput"/> class.
        /// </summary>
        /// <param name="trackingId">tracking id.</param>
        /// <param name="azureResourceInfo">Azure resource info.</param>
        public NextStageInput(string trackingId, AzureResourceInfo azureResourceInfo)
            : this(trackingId, azureResourceInfo, retryAttempt: 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NextStageInput"/> class.
        /// </summary>
        /// <param name="trackingId">tracking id.</param>
        /// <param name="azureResourceInfo">Azure resource info.</param>
        /// <param name="retryAttempt">retry attempt.</param>
        public NextStageInput(string trackingId, AzureResourceInfo azureResourceInfo, int retryAttempt)
        {
            TrackingId = trackingId;
            AzureResourceInfo = azureResourceInfo;
            RetryAttempt = retryAttempt;
            Version = CurrentVersion;
        }

        /// <summary>
        /// Gets or sets the tracking id for next continuation phase.
        /// </summary>
        public string TrackingId { get; set; }

        /// <summary>
        /// Gets or sets the compute azure resource info.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the retry attempt count.
        /// </summary>
        public int RetryAttempt { get; set; }

        /// <summary>
        /// Gets or sets the next stage input version. It increments by one for every breaking change.
        /// </summary>
        public int Version { get; set; }
    }
}