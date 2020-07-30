// <copyright file="StorageSharedIdentity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ManagedIdentityProvider
{
    /// <summary>
    /// Represents the Storage Shared Identity Response.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class StorageSharedIdentity
    {
        /// <summary>
        /// Gets or sets the resource ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the resource type, usually "Microsoft.Storage/storageAccounts/sharedIdentities".
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the name, usually "default".
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the shared identity properties.
        /// </summary>
        public StorageSharedIdentityProperties Properties { get; set; }
    }
}
