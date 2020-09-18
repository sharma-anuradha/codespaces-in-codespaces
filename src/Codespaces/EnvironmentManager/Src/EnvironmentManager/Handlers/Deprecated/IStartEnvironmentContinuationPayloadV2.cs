// <copyright file="IStartEnvironmentContinuationPayloadV2.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    public interface IStartEnvironmentContinuationPayloadV2 : IEnvironmentContinuationPayload
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
        /// Gets or sets start or export cloud environment parameters.
        /// </summary>
        public CloudEnvironmentParameters CloudEnvironmentParameters { get; set; }

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
        /// Gets or sets a value indicating action that enviornment is to take.
        /// </summary>
        public StartEnvironmentInputActionState ActionState { get; set; }
    }
}
