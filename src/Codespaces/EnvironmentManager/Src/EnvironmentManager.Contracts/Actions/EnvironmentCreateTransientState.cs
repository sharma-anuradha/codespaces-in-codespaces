// <copyright file="EnvironmentCreateTransientState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Transitent state to track properties required for exception handling in Environment Create Action.
    /// </summary>
    public class EnvironmentCreateTransientState
    {
        /// <summary>
        /// Gets or sets the environment Id.
        /// </summary>
        public Guid EnvironmentId { get; set; }

        /// <summary>
        /// Gets or sets the compute resource id allocated during environment creation.
        /// </summary>
        public Guid? AllocatedComputeId { get; set; }

        /// <summary>
        /// Gets or sets the storage resource id allocated during environment creation.
        /// </summary>
        public Guid? AllocatedStorageId { get; set; }

        /// <summary>
        /// Gets or sets the os disk resource id allocated during environment creation.
        /// </summary>
        public Guid? AllocatedOsDiskId { get; set; }

        /// <summary>
        /// Gets or sets the liveshare workspace id allocated during environment creation.
        /// </summary>
        public string AllocatedLiveshareWorkspaceId { get; set; }
    }
}
