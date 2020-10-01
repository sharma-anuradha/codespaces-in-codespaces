// <copyright file="EnvironmentFinalizeUpdateActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment Finalize Update Action Input.
    /// </summary>
    public class EnvironmentFinalizeUpdateActionInput : EnvironmentBaseFinalizeActionInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentFinalizeUpdateActionInput"/> class.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        public EnvironmentFinalizeUpdateActionInput(Guid environmentId)
            : base(environmentId)
        {
        }
    }
}
