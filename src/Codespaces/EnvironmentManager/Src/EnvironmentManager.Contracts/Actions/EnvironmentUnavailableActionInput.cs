// <copyright file="EnvironmentUnavailableActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Fail Action Input.
    /// </summary>
    public class EnvironmentUnavailableActionInput : IEntityActionIdInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentUnavailableActionInput"/> class.
        /// </summary>
        /// <param name="id">The environment id.</param>
        public EnvironmentUnavailableActionInput(Guid id, string reason)
        {
            Id = id;
            Reason = reason;
        }

        /// <inheritdoc/>
        public Guid Id { get; }

        /// <summary>
        /// The reason for failing the environment.
        /// </summary>
        public string Reason { get; set; }
    }
}
