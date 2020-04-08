// <copyright file="CreateEnvironmentContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models
{
    /// <summary>
    /// ResumeEnvironmentContinuationInput.
    /// </summary>
    public class CreateEnvironmentContinuationInput : ContinuationOperationInput
    {
        /// <summary>
        /// Gets or sets when was environment state was updated last.
        /// </summary>
        public DateTime LastStateUpdated { get; set; }

        /// <summary>
        /// Gets or sets cloud environment options.
        /// </summary>
        public CloudEnvironmentOptions CloudEnvironmentOptions { get; set; }

        /// <summary>
        /// Gets or sets start cloud environment parameters.
        /// </summary>
        public StartCloudEnvironmentParameters StartCloudEnvironmentParameters { get; set; }

        /// <summary>
        /// Gets or sets current state.
        /// </summary>
        public CreateEnvironmentContinuationInputState CurrentState { get;  set; }

        /// <summary>
        /// Gets or sets the compute resource for environment.
        /// </summary>
        public EnvironmentContinuationInputResource ComputeResource { get; set; }

        /// <summary>
        /// Gets or sets the storage resource for environment.
        /// </summary>
        public EnvironmentContinuationInputResource StorageResource { get; set; }
    }
}