// <copyright file="CreateSecretBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager
{
    /// <summary>
    /// Create secret body.
    /// </summary>
    public class CreateSecretBody
    {
        /// <summary>
        /// Gets or sets secret name.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "secretName")]
        public string SecretName { get; set; }

        /// <summary>
        /// Gets or sets secret value.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "value")]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets secret type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(Required = Required.Always, PropertyName = "type")]
        public SecretType Type { get; set; }

        /// <summary>
        /// Gets or sets notes.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "notes")]
        public string Notes { get; set; }

        /// <summary>
        /// Gets or sets secret filters.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "filters")]
        public IEnumerable<SecretFilterBody> Filters { get; set; }
    }
}