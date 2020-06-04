// <copyright file="UpdateSecretBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager
{
    /// <summary>
    /// Update secret body.
    /// </summary>
    public class UpdateSecretBody
    {
        /// <summary>
        /// Gets or sets secret name.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "secretName")]
        public string SecretName { get; set; }

        /// <summary>
        /// Gets or sets secret value.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "value")]
        public string Value { get; set; }

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