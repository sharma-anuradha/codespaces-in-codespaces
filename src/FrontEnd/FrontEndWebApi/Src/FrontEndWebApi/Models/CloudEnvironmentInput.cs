// <copyright file="CloudEnvironmentInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// The REST API body for registering/creating a new Environment.
    /// </summary>
    public class CloudEnvironmentInput
    {
        /// <summary>
        /// Gets or sets teh environment type.
        /// </summary>
        [Required]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the environment's friendly name.
        /// </summary>
        [Required]
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to create a file share for this environment.
        /// </summary>
        public bool CreateFileShare { get; set; }

        /// <summary>
        /// Gets or sets the environment seed info.
        /// </summary>
        public SeedInfoInput Seed { get; set; }

        /// <summary>
        /// Gets or sets the environment personalization info.
        /// </summary>
        public PersonalizationInfo Personalization { get; set; }

        /// <summary>
        /// Gets or sets the environment container image.
        /// </summary>
        public string ContainerImage { get; set; }

        /// <summary>
        /// Gets or sets the environment Live Share connection info.
        /// </summary>
        public ConnectionInfoInput Connection { get; set; }

        /// <summary>
        /// Gets or sets the enviroment platform.
        /// </summary>
        [Obsolete("This is now implied by SkuName", false)]
        public string Platform { get; set; }

        /***** NEW *****/

        /// <summary>
        /// Gets or sets the azure location for this environment.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the cloud environment sku name.
        /// </summary>
        public string SkuName { get; set; }
    }
}