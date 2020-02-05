// <copyright file="PlanResourceUpdateBody.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// JSON body for updating plan resource properties from RPSaaS.
    /// </summary>
    public class PlanResourceUpdateBody
    {
        /// <summary>
        /// Gets or sets the properties object.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "properties")]
        public PlanResourceProperties Properties { get; set; }
    }
}
