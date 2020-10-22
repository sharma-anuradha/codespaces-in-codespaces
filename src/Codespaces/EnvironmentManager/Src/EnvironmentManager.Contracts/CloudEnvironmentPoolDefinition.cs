// <copyright file="CloudEnvironmentPoolDefinition.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Subscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment pool definition.
    /// </summary>
    public class CloudEnvironmentPoolDefinition
    {
        /// <summary>
        /// Gets or sets the definition code.
        /// </summary>
        [JsonProperty(PropertyName = "code")]
        public string Code { get; set; }
    }
}