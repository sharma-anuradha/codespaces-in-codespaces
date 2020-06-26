// <copyright file="CreateResourceWithComponentInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Resource provider create input.
    /// </summary>
    public class CreateResourceWithComponentInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets resource id.
        /// </summary>
        public string ResourceId { get; set; }

        /// <summary>
        /// Gets or sets azure sku name.
        /// </summary>
        public string AzureSkuName { get; set; }

        /// <summary>
        /// Gets or sets resource subscription.
        /// </summary>
        public Guid AzureSubscription { get; set; }

        /// <summary>
        /// Gets or sets resource group.
        /// </summary>
        public string AzureResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets resource tag.
        /// </summary>
        public IDictionary<string, string> ResourceTags { get; set; }

        /// <summary>
        /// Gets or sets custom components.
        /// </summary>
        public List<ResourceComponent> CustomComponents { get; set; }

        /// <summary>
        /// Gets or sets input to create custom components.
        /// </summary>
        public Dictionary<string, ComponentInput> CustomComponentInputs { get; set; }

        /// <summary>
        /// Gets or sets resource creation stage.
        /// </summary>
        public ResourceCreationState Stage { get; set; }
    }
}