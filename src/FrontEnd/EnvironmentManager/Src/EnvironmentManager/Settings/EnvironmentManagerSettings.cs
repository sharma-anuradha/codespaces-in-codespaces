// <copyright file="EnvironmentManagerSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    public class EnvironmentManagerSettings
    {
        /// <summary>
        /// Gets or sets the Max Environments Per Plan.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int MaxEnvironmentsPerPlan { get; set; }

        /// <summary>
        /// Gets or sets the name of the blob container that the Environment Manager
        /// can use for distributed leases.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string LeaseContainerName { get; set; }
    }
}
