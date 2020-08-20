// <copyright file="StartEnvironmentContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models
{
    /// <summary>
    /// Create Environment Continuation Input.
    /// </summary>
    public class StartEnvironmentContinuationInput : ContinuationOperationInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartEnvironmentContinuationInput"/> class.
        /// </summary>
        /// <param name="options">Cloud environment options.</param>
        /// <param name="createNew">create new environment or resume.</param>
        public StartEnvironmentContinuationInput(CloudEnvironmentOptions options, bool createNew)
        {
            CloudEnvironmentOptions = options;
            CreateNew = createNew;
        }

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
        public StartEnvironmentContinuationInputState CurrentState { get; set; }

        /// <summary>
        /// Gets or sets the compute resource for environment.
        /// </summary>
        public EnvironmentContinuationInputResource ComputeResource { get; set; }

        /// <summary>
        /// Gets or sets the osdisk resource for environment.
        /// </summary>
        public EnvironmentContinuationInputResource OSDiskResource { get; set; }

        /// <summary>
        /// Gets or sets the storage resource for environment.
        /// </summary>
        public EnvironmentContinuationInputResource StorageResource { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a new environment is getting created or resumed.
        /// </summary>
        public bool CreateNew { get; set; }
    }
}