// <copyright file="EnvironmentBaseFinalizeActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Base Finalize Action Input.
    /// </summary>
    public abstract class EnvironmentBaseFinalizeActionInput : IEntityActionIdInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentBaseFinalizeActionInput"/> class.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        public EnvironmentBaseFinalizeActionInput(Guid environmentId)
        {
            Id = environmentId;
        }

        /// <summary>
        /// Gets or sets new storage that should be swapped in.
        /// </summary>
        public Guid StorageResourceId { get; set; }

        /// <summary>
        /// Gets or sets archive storage resource id if waking from archive.
        /// </summary>
        public Guid? ArchiveStorageResourceId { get; set; }

        /// <inheritdoc/>
        public Guid Id { get; }
    }
}
