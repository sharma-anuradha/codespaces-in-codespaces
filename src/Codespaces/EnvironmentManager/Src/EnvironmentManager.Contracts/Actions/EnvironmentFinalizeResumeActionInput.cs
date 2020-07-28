// <copyright file="EnvironmentFinalizeResumeActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Finalize Resume Action Input.
    /// </summary>
    public class EnvironmentFinalizeResumeActionInput : IEntityActionIdInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentFinalizeResumeActionInput"/> class.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        public EnvironmentFinalizeResumeActionInput(Guid environmentId)
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
