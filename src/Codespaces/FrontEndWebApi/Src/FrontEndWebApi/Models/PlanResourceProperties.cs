// <copyright file="PlanResourceProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// Plan resource properties bag from RPaaS.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class PlanResourceProperties
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the auto suspend timeout of environments under the plan.
        /// </summary>
        public int? DefaultAutoSuspendDelayMinutes { get; set; }

        /// <summary>
        /// Gets or sets the environment sku name under the plan.
        /// </summary>
        public string DefaultEnvironmentSku { get; set; }

        /// <summary>
        /// Gets or sets the codespace sku name under the plan.
        /// </summary>
        public string DefaultCodespaceSku { get; set; }

        /// <summary>
        /// Gets or sets the vnet properties to create environments in this plan.
        /// </summary>
        public VnetProperties VnetProperties { get; set; }
    }
}
