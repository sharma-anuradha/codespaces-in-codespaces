// <copyright file="ResourcePoolKeyVaultDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
{
    /// <summary>
    /// KeyVault details for scaling input.
    /// </summary>
    public class ResourcePoolKeyVaultDetails : ResourcePoolResourceDetails
    {
        private const string ResourceType = "KeyVault";

        /// <inheritdoc/>
        public override string GetPoolDefinition()
        {
            return $"{ResourceType}__{SkuName}__{Location}".GetDeterministicHashCode();
        }

        /// <inheritdoc/>
        public override IDictionary<string, string> GetPoolDimensions()
        {
            return new Dictionary<string, string>
            {
                [ResourcePoolDimensionsKeys.SkuName] = SkuName,
                [ResourcePoolDimensionsKeys.Location] = Location.ToString(),
            };
        }

        /// <inheritdoc/>
        public override string GetPoolVersionDefinition()
        {
            return $"{ResourceType}__{SkuName}__{Location}".GetDeterministicHashCode();
        }
    }
}
