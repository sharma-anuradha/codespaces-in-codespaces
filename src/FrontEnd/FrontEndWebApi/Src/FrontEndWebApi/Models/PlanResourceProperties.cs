// <copyright file="PlanResourceProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    public class PlanResourceProperties
    {
       [JsonProperty(Required = Required.Default, PropertyName = "userId")]
       public string UserId { get; set; }
    }
}
