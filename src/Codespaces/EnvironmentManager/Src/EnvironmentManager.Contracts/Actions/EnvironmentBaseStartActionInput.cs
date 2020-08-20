// <copyright file="EnvironmentBaseStartActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment base start action input.
    /// </summary>
    public abstract class EnvironmentBaseStartActionInput : IEntityActionIdInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentBaseStartActionInput"/> class.
        /// </summary>
        /// <param name="id">Target environment id.</param>
        public EnvironmentBaseStartActionInput(Guid id)
        {
            Id = id;
        }

        /// <inheritdoc/>
        public Guid Id { get; }
    }
}