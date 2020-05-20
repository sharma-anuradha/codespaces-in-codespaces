// <copyright file="ScopedCreateSecretBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager
{
    /// <summary>
    /// Scoped create secret request body.
    /// </summary>
    public class ScopedCreateSecretBody
    {
        /// <summary>
        /// Gets or sets secret scope.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(Required = Required.Always, PropertyName = "scope")]
        public SecretScope Scope { get; set; }

        /// <summary>
        /// Gets or sets secret name.
        /// </summary>
        [StringLength(100)]
        [JsonProperty(Required = Required.Always, PropertyName = "secretName")]
        public string SecretName { get; set; }

        /// <summary>
        /// Gets or sets secret value.
        /// </summary>
        [StringLength(1024)]
        [JsonProperty(Required = Required.Always, PropertyName = "value")]
        public string Value { get; set; }

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