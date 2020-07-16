// <copyright file="PlanResourceEncryptionProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// Encryption properties associated with the plan.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class PlanResourceEncryptionProperties
    {
        /// <summary>
        /// Gets or sets the encryption properties.
        /// </summary>
        public PlanResourceKeyVaultProperties KeyVaultProperties { get; set; }

        /// <summary>
        /// Gets or sets the KeySource, either "Microsoft.Codespaces" or "Microsoft.KeyVault".
        /// </summary>
        public string KeySource { get; set; }
    }
}
