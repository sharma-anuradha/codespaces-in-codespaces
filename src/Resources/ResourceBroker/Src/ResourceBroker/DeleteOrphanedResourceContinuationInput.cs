// <copyright file="DeleteOrphanedResourceContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Delete orphaned resource continuation input.
    /// </summary>
    public class DeleteOrphanedResourceContinuationInput : ContinuationOperationInput
    {
        /// <summary>
        /// Gets or sets resource type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ResourceType Type { get; set; }

        /// <summary>
        /// Gets or sets azure resource info.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the resource tags.
        /// </summary>
        public IDictionary<string, string> ResourceTags { get; set; }

        /// <summary>
        /// Gets or sets the Azure location.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureLocation AzureLocation { get; set; }
    }
}
