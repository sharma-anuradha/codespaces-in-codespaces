// <copyright file="AccountInputProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    public class AccountInputProperties
    {
        [JsonProperty(Required = Required.Default, PropertyName = "sku")]
        public Sku Plan { get; set; }
    }
}
