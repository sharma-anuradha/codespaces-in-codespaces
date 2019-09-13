// <copyright file="CreateResourceContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// Create compute continuation input.
    /// </summary>
    public class CreateResourceContinuationInput : ContinuationOperationInput
    {
        /// <summary>
        /// Gets or sets the details of pool.
        /// </summary>
        public ResourcePoolResourceDetails ResourcePoolDetails { get; set; }

        /// <summary>
        /// Gets or sets the pool type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ResourceType Type { get; set; }
    }
}
