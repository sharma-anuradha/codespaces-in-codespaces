// <copyright file="BillingStateChange.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public class BillingStateChange
    {
        [JsonProperty(Required = Required.Always, PropertyName = "oldValue")]
        public string OldValue { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "newValue")]
        public string NewValue { get; set; }
    }
}
