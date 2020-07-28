// <copyright file="EnvironmentCreateActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Create Action Input.
    /// </summary>
    public class EnvironmentCreateActionInput
    {
        /// <summary>
        /// Gets or sets environment create options.
        /// </summary>
        public EnvironmentCreateDetails Details { get; set; }

        /// <summary>
        /// Gets or sets plan details.
        /// </summary>
        public VsoPlan Plan { get; set; }

        /// <summary>
        /// Gets or sets metrics info.
        /// </summary>
        public MetricsInfo MetricsInfo { get; set; }

        /// <summary>
        /// Gets or sets start env params.
        /// </summary>
        public StartCloudEnvironmentParameters StartEnvironmentParams { get; set; }
    }
}
