// <copyright file="EnvironmentDeleteActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts.Actions
{
    /// <summary>
    /// Environment Delete Action Input.
    /// </summary>
    public class EnvironmentDeleteActionInput : IEntityActionIdInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentDeleteActionInput"/> class.
        /// </summary>
        /// <param name="environmentId">Target environment Id.</param>
        public EnvironmentDeleteActionInput(Guid environmentId)
        {
            Id = environmentId;
        }

        /// <summary>
        /// Gets or sets the compute resource id allocated to the environment.
        /// This is optional if the compute is already persisted on the environment record.
        /// </summary>
        public Guid? AllocatedComputeId { get; set; }

        /// <summary>
        /// Gets or sets the storage resource id allocated to the environment.
        /// This is optional if the storage is already persisted on the environment record.
        /// </summary>
        public Guid? AllocatedStorageId { get; set; }

        /// <summary>
        /// Gets or sets the os disk resource id allocated to the environment.
        /// This is optional if the storage is already persisted on the environment record.
        /// </summary>
        public Guid? AllocatedOsDiskId { get; set; }

        /// <summary>
        /// Gets or sets the liveshare workspace id allocated to the environment.
        /// This is optional if the lievshare workspace id is already persisted on the environment record.
        /// </summary>
        public string AllocatedLiveshareWorkspaceId { get; set; }

        /// <inheritdoc/>
        public Guid Id { get; }
    }
}
