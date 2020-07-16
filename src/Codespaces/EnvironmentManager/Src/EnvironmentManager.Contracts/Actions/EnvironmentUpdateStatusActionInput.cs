// <copyright file="EnvironmentUpdateStatusActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Update Status Action Input.
    /// </summary>
    public class EnvironmentUpdateStatusActionInput : IEntityActionIdInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentUpdateStatusActionInput"/> class.
        /// </summary>
        /// <param name="id">Target cloud environment id.</param>
        public EnvironmentUpdateStatusActionInput(Guid id)
        {
            Id = id;
        }

        /// <summary>
        /// Gets or sets new state.
        /// </summary>
        public CloudEnvironmentState NewState { get; set; }

        /// <summary>
        /// Gets or sets trigger.
        /// </summary>
        public string Trigger { get; set; }

        /// <summary>
        /// Gets or sets Reason.
        /// </summary>
        public string Reason { get; set; }

        /// <inheritdoc/>
        public Guid Id { get; }
    }
}
