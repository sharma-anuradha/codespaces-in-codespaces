// <copyright file="EnvironmentResumeActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment resume action input.
    /// </summary>
    public class EnvironmentResumeActionInput : IEntityActionIdInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentResumeActionInput"/> class.
        /// </summary>
        /// <param name="id">Target environment id.</param>
        public EnvironmentResumeActionInput(Guid id)
        {
            Id = id;
        }

        /// <summary>
        /// Gets or sets start env params.
        /// </summary>
        public StartCloudEnvironmentParameters StartEnvironmentParams { get; set; }

        /// <inheritdoc/>
        public Guid Id { get; }
    }
}