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
    public class EnvironmentFinalizeResumeActionInput : EnvironmentBaseFinalizeActionInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentFinalizeResumeActionInput"/> class.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        public EnvironmentFinalizeResumeActionInput(Guid environmentId)
            : base(environmentId)
        {
        }
    }
}
