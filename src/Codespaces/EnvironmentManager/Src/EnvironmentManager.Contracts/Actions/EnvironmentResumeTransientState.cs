// <copyright file="EnvironmentResumeTransientState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Transitent state to track properties required for exception handling in Environment Resume Action.
    /// </summary>
    public class EnvironmentResumeTransientState
    {
        /// <summary>
        /// Gets or sets the compute resource id allocated during environment resume.
        /// </summary>
        public Guid AllocatedComputeId { get; set; }

        /// <summary>
        /// Gets or sets the environment state to be tracked during environment resume.
        /// </summary>
        public CloudEnvironmentState CloudEnvironmentState { get; set; }
    }
}
