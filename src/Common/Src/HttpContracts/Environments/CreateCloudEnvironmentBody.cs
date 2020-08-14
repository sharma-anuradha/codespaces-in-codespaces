// <copyright file="CreateCloudEnvironmentBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments
{
    /// <summary>
    /// The REST API body for registering/creating a new Environment.
    /// </summary>
    public class CreateCloudEnvironmentBody
    {
        /// <summary>
        /// Gets or sets the environment type.
        /// </summary>
        [Required]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the environment's friendly name.
        /// </summary>
        [Required]
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the experimental features requested by the client.
        /// </summary>
        public ExperimentalFeaturesBody ExperimentalFeatures { get; set; }

        /// <summary>
        /// Gets or sets features requested by the client.
        /// </summary>
        public Dictionary<string, string> Features { get; set; }

        /// <summary>
        /// Gets or sets the environment seed info.
        /// </summary>
        public SeedInfoBody Seed { get; set; }

        /// <summary>
        /// Gets or sets the environment personalization info.
        /// </summary>
        public PersonalizationInfoBody Personalization { get; set; }

        /// <summary>
        /// Gets or sets the environment container image.
        /// </summary>
        public string ContainerImage { get; set; }

        /// <summary>
        /// Gets or sets the environment Live Share connection info.
        /// </summary>
        public ConnectionInfoBody Connection { get; set; }

        /// <summary>
        /// Gets or sets the enviroment platform.
        /// </summary>
        [Obsolete("This is now implied by SkuName", false)]
        public string Platform { get; set; }

        /// <summary>
        /// Gets or sets the azure location for this environment.
        /// </summary>
        [Obsolete("This is now implied by Plan", false)]
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the cloud environment sku name.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the fully-qualified Azure resource id of the Plan object.
        /// </summary>
        public string PlanId { get; set; }

        /// <summary>
        /// Gets or sets the auto shutdown time the user specified.
        /// </summary>
        public int AutoShutdownDelayMinutes { get; set; }

        /// <summary>
        /// Gets or sets the secrets from Create/Resume request.
        /// </summary>
        public IEnumerable<SecretDataBody> Secrets { get; set; }
    }
}