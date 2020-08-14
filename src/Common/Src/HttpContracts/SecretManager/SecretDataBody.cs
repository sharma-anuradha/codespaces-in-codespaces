// <copyright file="SecretDataBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager
{
    /// <summary>
    /// Data contract for sending user secrets to the VM.
    /// </summary>
    public class SecretDataBody
    {
        /// <summary>
        /// Gets or sets secret type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(Required = Required.Always, PropertyName = "type")]
        public SecretType Type { get; set; }

        /// <summary>
        /// Gets or sets secret name.
        /// Valid characters:
        ///     Only alpha-numeric charaters and underscore,
        ///     Cannot start with a number,
        ///     Cannot start with the word CLOUDENV and CODESPACE
        ///     Length must be between 1 and 200 charatcers.
        /// </summary>
        [RegularExpression("^(?!CLOUDENV.*$)(?!CODESPACE.*$)[a-zA-Z_][a-zA-Z0-9_]{0,199}$")]
        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets secret value.
        /// </summary>
        [StringLength(1024, MinimumLength = 1)]
        [JsonProperty(Required = Required.Always, PropertyName = "value")]
        public string Value { get; set; }
    }
}
