// <copyright file="CloudEnvironmentResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments
{
    /// <summary>
    /// The environment registration REST API result.
    /// </summary>
    public class CloudEnvironmentResult
    {
        private string state;

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
        public string State
        {
            get { return state == "Archived" ? "Shutdown" : state; }
            set { state = value; }
        }

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
        /// Gets or sets session paths for folders opened in an environment.
        /// </summary>
        public List<string> RecentFolders { get; set; }

        /// <summary>
        /// Gets or sets the last active date.
        /// </summary>
        public DateTime Active { get; set; }

        /// <summary>
        /// Gets or sets the environment platform.
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// Gets or sets the azure location of the environment.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the fully-qualified Azure resource id of the Plan object.
        /// </summary>
        public string PlanId { get; set; }

        /// <summary>
        /// Gets or sets the auto shutdown time the user specified.
        /// </summary>
        public int AutoShutdownDelayMinutes { get; set; }

        /// <summary>
        /// Gets or sets the name of the Sku.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the display name of the Sku.
        /// </summary>
        public string SkuDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the Last State Update reason.
        /// </summary>
        public string LastStateUpdateReason { get; set; }

        /// <summary>
        /// Gets or sets the last used time.
        /// </summary>
        public DateTime? LastUsed { get; set; }

        /// <summary>
        /// Gets or sets the features.
        /// </summary>
        public Dictionary<string, string> Features { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the environment has unpushed git changes.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool HasUnpushedGitChanges { get; set; }
    }
}
