// <copyright file="PlanResourceIdentity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// Managed identity properties from RPaaS.
    /// </summary>
    public class PlanResourceIdentity
    {
        /// <summary>
        /// Gets or sets the managed identity type.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the principal ID of the managed identity. Not stored by RPaaS.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "principalId", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? PrincipalId { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID of the managed identity. Not stored by RPaaS.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "tenantId", NullValueHandling = NullValueHandling.Ignore)]
        public Guid? TenantId { get; set; }
    }
}
