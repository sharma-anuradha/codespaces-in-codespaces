// <copyright file="PlanResourceList.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// JSON body properties from RPaaS.
    /// </summary>
    public class PlanResourceList
    {
        /// <summary>
        /// Gets or sets the <see cref="PlanResource"/> list.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "value")]
        public IEnumerable<PlanResource> Value { get; set; }
    }
}
