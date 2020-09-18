// <copyright file="SuspendEnvironmentPayload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    public class SuspendEnvironmentPayload : JobPayload
    {
        /// <summary>
        /// Gets or sets the environment Id.
        /// </summary>
        public string EnvironmentId { get; set; }
    }
}
