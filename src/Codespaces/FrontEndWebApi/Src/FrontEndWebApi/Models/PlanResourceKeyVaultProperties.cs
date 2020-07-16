// <copyright file="PlanResourceKeyVaultProperties.cs" company="Microsoft">
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
    /// KeyVault properties associated with the plan resource.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class PlanResourceKeyVaultProperties
    {
        /// <summary>
        /// Gets or sets the key name.
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// Gets or sets the key version.
        /// </summary>
        public string KeyVersion { get; set; }

        /// <summary>
        /// Gets or sets the KeyVault URI.
        /// </summary>
        public string KeyVaultUri { get; set; }
    }
}
