// <copyright file="EnvironmentStartTransientState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Transient state to track properties required for exception handling in Environment Start Action.
    /// </summary>
    public abstract class EnvironmentStartTransientState
    {
        /// <summary>
        /// Gets or sets the compute resource id allocated during environment start action.
        /// </summary>
        public Guid AllocatedComputeId { get; set; }

        /// <summary>
        /// Gets or sets the environment state to be tracked during environment start action.
        /// </summary>
        public CloudEnvironmentState CloudEnvironmentState { get; set; }
    }
}