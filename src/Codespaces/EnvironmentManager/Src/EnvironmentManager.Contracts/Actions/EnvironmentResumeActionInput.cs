// <copyright file="EnvironmentResumeActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment resume action input.
    /// </summary>
    public class EnvironmentResumeActionInput : EnvironmentBaseStartActionInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentResumeActionInput"/> class.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        public EnvironmentResumeActionInput(Guid environmentId)
            : base(environmentId)
        {
        }

        /// <summary>
        /// Gets or sets start env params.
        /// </summary>
        public StartCloudEnvironmentParameters StartEnvironmentParams { get; set; }
    }
}