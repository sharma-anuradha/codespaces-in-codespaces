// <copyright file="TrackedVirtualMachineProviderCreateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// Create compute continuation input.
    /// </summary>
    public class CreateResourceContinuationInput
    {
        /// <summary>
        /// Gets or sets the name of the sku.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ResourceType Type { get; set; }

        /// <summary>
        /// Gets or sets the location of the resource.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the instance id of the persisted record.
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Gets or sets the subscription.
        /// </summary>
        public string Subscription { get; set; }

        /// <summary>
        /// Gets or sets the resource group.
        /// </summary>
        public string ResourceGroup { get; set; }
    }
}
