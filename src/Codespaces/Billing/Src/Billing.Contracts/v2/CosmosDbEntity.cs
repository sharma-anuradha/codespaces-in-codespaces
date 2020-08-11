// <copyright file="CosmosDbEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VsSaaS.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
    /// <summary>
    /// A Tagged Entity with a fixed partition key property.
    /// </summary>
    public class CosmosDbEntity : TaggedEntity
    {
        /// <summary>
        /// Gets or sets the partitionKey. This corresponds to the planId of the environment that this plan belongs to.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }
    }
}
