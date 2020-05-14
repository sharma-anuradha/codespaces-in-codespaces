// <copyright file="ScopedSecretResultBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager
{
    /// <summary>
    /// Scoped secret result http response corresponding to a stored secret.
    /// </summary>
    public class ScopedSecretResultBody
    {
        /// <summary>
        /// Gets or sets secret Id.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets last modified time.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "lastModified")]
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Gets or sets secret scope.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(Required = Required.Always, PropertyName = "scope")]
        public SecretScope Scope { get; set; }

        /// <summary>
        /// Gets or sets secret name.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "secretName")]
        public string SecretName { get; set; }

        /// <summary>
        /// Gets or sets secret type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(Required = Required.Always, PropertyName = "type")]
        public SecretType Type { get; set; }

        /// <summary>
        /// Gets or sets secret filters.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "filters")]
        public IDictionary<SecretFilterType, string> Filters { get; set; }
    }
}
