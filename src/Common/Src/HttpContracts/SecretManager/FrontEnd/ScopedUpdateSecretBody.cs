// <copyright file="ScopedUpdateSecretBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager
{
    /// <summary>
    /// Scoped update secret request body.
    /// </summary>
    public class ScopedUpdateSecretBody
    {
        /// <summary>
        /// Gets or sets secret scope.
        /// This is used to determine which secret store the secret lives in.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(Required = Required.Always, PropertyName = "scope")]
        public SecretScope Scope { get; set; }

        /// <summary>
        /// Gets or sets secret name to update.
        /// </summary>
        [StringLength(100)]
        [JsonProperty(Required = Required.Default, PropertyName = "secretName")]
        public string SecretName { get; set; }

        /// <summary>
        /// Gets or sets secret value to update.
        /// </summary>
        [StringLength(1024)]
        [JsonProperty(Required = Required.Default, PropertyName = "value")]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets secret filters to update.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "filters")]
        public IDictionary<SecretFilterType, string> Filters { get; set; }
    }
}