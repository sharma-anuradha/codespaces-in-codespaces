// <copyright file="EnvironmentUpdateActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment update action input.
    /// </summary>
    public class EnvironmentUpdateActionInput : EnvironmentBaseStartActionInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentUpdateActionInput"/> class.
        /// </summary>
        /// <param name="id">Target environment id.</param>
        public EnvironmentUpdateActionInput(Guid id) : base(id)
        {
        }

        /// <summary>
        /// Gets or sets start env parameters.
        /// </summary>
        public CloudEnvironmentParameters CloudEnvironmentParameters { get; set; }
    }
}
