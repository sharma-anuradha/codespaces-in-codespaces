// <copyright file="KeyVaultResourceNotFoundException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models
{
    /// <summary>
    /// KeyVault resource not found exception.
    /// </summary>
    public class KeyVaultResourceNotFoundException : KeyVaultProviderException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultResourceNotFoundException"/> class.
        /// </summary>
        /// <param name="resourceId">The resource id.</param>
        /// <param name="inner">The inner exception.</param>
        public KeyVaultResourceNotFoundException(
            Guid resourceId,
            Exception inner = null)
            : base($"No KeyVault resources found with id '{resourceId}'.", inner)
        {
            ResourceId = resourceId;
        }

        /// <summary>
        /// Gets the resource id.
        /// </summary>
        public Guid ResourceId { get; }
    }
}