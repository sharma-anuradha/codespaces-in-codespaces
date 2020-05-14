// <copyright file="UpdateSecretBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
        [JsonProperty(Required = Required.Always, PropertyName = "secretName")]
        public string SecretName { get; set; }

        /// <summary>
        /// Gets or sets secret value.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "value")]
        public string Value { get; set; }
    }
}