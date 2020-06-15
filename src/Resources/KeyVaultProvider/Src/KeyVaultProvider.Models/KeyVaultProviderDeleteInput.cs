// <copyright file="KeyVaultProviderDeleteInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models
{
    /// <summary>
    /// Input for deleting a KeyVault.
    /// </summary>
    public class KeyVaultProviderDeleteInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets he azure resource to be deleted.
        /// </summary>
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the virtual machine location.
        /// </summary>
        public AzureLocation AzureLocation { get; set; }
    }
}