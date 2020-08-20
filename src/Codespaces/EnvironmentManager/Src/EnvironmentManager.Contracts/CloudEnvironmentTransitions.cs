// <copyright file="CloudEnvironmentTransitions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Transtions of cloud environments.
    /// </summary>
    public class CloudEnvironmentTransitions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentTransitions"/> class.
        /// </summary>
        public CloudEnvironmentTransitions()
        {
            Archiving = new TransitionState();
            Provisioning = new TransitionState();
            Resuming = new TransitionState();
            ShuttingDown = new TransitionState();
            Exporting = new TransitionState();
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

        /// <summary>
        /// Gets or sets the resume environment transitions.
        /// </summary>
        [JsonProperty(PropertyName = "resumeEnvironment")]
        public TransitionState Resuming { get; set; }

        /// <summary>
        /// Gets or sets the shutdown environment transitions.
        /// </summary>
        [JsonProperty(PropertyName = "shutdownEnvironment")]
        public TransitionState ShuttingDown { get; set; }

        /// <summary>
        /// Gets or sets the export environment transitions.
        /// </summary>
        [JsonProperty(PropertyName = "exportEnvironment")]
        public TransitionState Exporting { get; set; }
    }
}
