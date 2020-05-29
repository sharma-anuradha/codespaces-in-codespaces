// <copyright file="StartEnvironmentContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// Start compute continuation input.
    /// </summary>
    public class StartEnvironmentContinuationInput : ContinuationOperationInput
    {
        /// <summary>
        /// Gets or sets Environment Id.
        /// </summary>
        public Guid? EnvironmentId { get; set; }

        /// <summary>
        /// Gets or sets the storage resource id.
        /// </summary>
        public Guid? StorageResourceId { get; set; }

        /// <summary>
        /// Gets or sets the archive storage resource id.
        /// </summary>
        public Guid? ArchiveStorageResourceId { get; set; }

        /// <summary>
        /// Gets or sets the compute environment variables.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }

        /// <summary>
        /// Gets or sets the user secrets.
        /// </summary>
        public IEnumerable<UserSecretData> UserSecrets { get; set; }

        /// <summary>
        /// Gets or sets the os disk source id.
        /// </summary>
        public Guid? OSDiskResourceId { get; set; }
    }
}
