// <copyright file="KeyVaultProviderCreateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models
{
    /// <summary>
    /// Input for KeyVault creation.
    /// </summary>
    public class KeyVaultProviderCreateInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets the Resource Id.
        /// </summary>
        public string ResourceId { get; set; }

        /// <summary>
        /// Gets or sets keyvault subscription.
        /// </summary>
        public string AzureSubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets tenantid for the keyvault.
        /// </summary>
        public string AzureTenantId { get; set; }

        /// <summary>
        /// Gets or sets objectId for keyvault access policy.
        /// </summary>
        public string AzureObjectId { get; set; }

        /// <summary>
        /// Gets or sets keyvault  location.
        /// </summary>
        public AzureLocation AzureLocation { get; set; }

        /// <summary>
        /// Gets or sets keyvault resource group.
        /// </summary>
        public string AzureResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets keyvault sku.
        /// </summary>
        public string AzureSkuName { get; set; }

        /// <summary>
        /// Gets or sets resource tags that should be added to the resource.
        /// </summary>
        public IDictionary<string, string> ResourceTags { get; set; }
    }
}