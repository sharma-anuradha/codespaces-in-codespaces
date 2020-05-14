// <copyright file="ResourceSecretsResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager
{
    /// <summary>
    /// Response with all the secrets under a resource.
    /// </summary>
    public class ResourceSecretsResult
    {
        /// <summary>
        /// Gets or sets resource id.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "resourceId")]
        public Guid ResourceId { get; set; }

        /// <summary>
        /// Gets or sets secrets.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "userSecrets")]
        public IEnumerable<SecretResult> UserSecrets { get; set; }
    }
}