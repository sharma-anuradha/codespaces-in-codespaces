// <copyright file="CloudEnvironmentTansitions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Transtions of cloud environments.
    /// </summary>
    public class CloudEnvironmentTansitions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentTansitions"/> class.
        /// </summary>
        public CloudEnvironmentTansitions()
        {
            Archiving = new TransitionState();
            Provisioning = new TransitionState();
        }

        /// <summary>
        /// Gets or sets the archiving transitions.
        /// </summary>
        [JsonProperty(PropertyName = "archiving")]
        public TransitionState Archiving { get; set; }

        /// <summary>
        /// Gets or sets the create environment transitions.
        /// </summary>
        [JsonProperty(PropertyName = "createEnvironment")]
        public TransitionState Provisioning { get; set; }
    }
}
