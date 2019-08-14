// <copyright file="CloudEnvironmentResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments
{
    /// <summary>
    /// The environment registration REST API result.
    /// </summary>
    public class CloudEnvironmentResult
    {
        /// <summary>
        /// Gets or sets the environment id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the environment type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the friendly name.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the created date.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the updated date.
        /// </summary>
        public DateTime Updated { get; set; }

        /// <summary>
        /// Gets or sets the owner id.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the container image.
        /// </summary>
        public string ContainerImage { get; set; }

        /// <summary>
        /// Gets or sets the environment seed info.
        /// </summary>
        public SeedInfoBody Seed { get; set; }

        /// <summary>
        /// Gets or sets the environment connection info.
        /// </summary>
        public ConnectionInfoBody Connection { get; set; }

        /// <summary>
        /// Gets or sets the last active date.
        /// </summary>
        public DateTime Active { get; set; }

        /// <summary>
        /// Gets or sets the environment platform.
        /// </summary>
        public string Platform { get; set; }
    }
}
