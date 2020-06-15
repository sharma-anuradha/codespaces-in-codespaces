// <copyright file="StorageInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Information about the environment's storage.
    /// </summary>
    public class StorageInfo
    {
        /// <summary>
        /// Gets or sets storage id (provided by the backend ResourceBroker).
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "storageId")]
        public string StorageId { get; set; }

        /// <summary>
        /// Gets or sets the storage kind.
        /// </summary>
        [Obsolete("This will no longer be known to the front-end", false)]
        [JsonProperty(Required = Required.Default, PropertyName = "storageKind")]
        public string StorageKind { get; set; }

        /// <summary>
        /// Gets or sets the underlying file share id.
        /// </summary>
        [Obsolete("This will no longer be known to the front-end", false)]
        [JsonProperty(Required = Required.Default, PropertyName = "fileShareName")]
        public string FileShareId { get; set; }
    }
}
