// <copyright file="CleanupResourceContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// Perform cleanup operations input.
    /// </summary>
    public class CleanupResourceContinuationInput : ContinuationOperationInput
    {
        /// <summary>
        /// Gets or sets the environment id.
        /// </summary>
        public string EnvironmentId { get; set; }
    }
}
