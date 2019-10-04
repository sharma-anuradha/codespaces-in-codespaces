// <copyright file="AccountResourceProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    public class AccountResourceProperties
    {
       [JsonProperty(Required = Required.Default, PropertyName = "userId")]
       public string UserId { get; set; }


       [JsonProperty(Required = Required.Default, PropertyName = "serviceUri")]
       public string ServiceUri { get; set; }
    }
}
