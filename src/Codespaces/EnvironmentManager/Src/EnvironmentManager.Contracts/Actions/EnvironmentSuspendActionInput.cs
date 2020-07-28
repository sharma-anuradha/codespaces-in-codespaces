// <copyright file="EnvironmentSuspendActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Force Suspend Action Input.
    /// </summary>
    public class EnvironmentSuspendActionInput : IEntityActionIdInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentSuspendActionInput"/> class.
        /// </summary>
        /// <param name="id">The environment id.</param>
        /// <param name="computeResourceId">
        /// The compute resource id allocated to the environment.
        /// This is optional if the compute is already persisted on the environment record.
        /// </param>
        public EnvironmentSuspendActionInput(Guid id, Guid computeResourceId = default)
        {
            Id = id;
            AllocatedComputeResourceId = computeResourceId;
        }

        /// <summary>
        /// Gets the compute resource id allocated to the environment.
        /// This is optional if the compute is already persisted on the environment record.
        /// </summary>
        public Guid AllocatedComputeResourceId { get; }

        /// <inheritdoc/>
        public Guid Id { get; }
    }
}