// <copyright file="SkuDisplayNameMapper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using AutoMapper;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{

    /// <summary>
    /// Maps SKU Display name from a CloudEnvironment.
    /// </summary>
    public class SkuDisplayNameMapper : IValueResolver<CloudEnvironment, CloudEnvironmentResult, string>
    {
        private readonly ISkuCatalog skuCatalog;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkuDisplayNameMapper"/> class.
        /// </summary>
        /// <param name="skuCatalog">The SKU catalog.</param>
        public SkuDisplayNameMapper(ISkuCatalog skuCatalog)
        {
            this.skuCatalog = skuCatalog;
        }

        /// <inheritdoc/>
        public string Resolve(CloudEnvironment source, CloudEnvironmentResult destination, string destMember, ResolutionContext context)
        {
            var skuName = source.SkuName;
            if (skuCatalog.CloudEnvironmentSkus.ContainsKey(skuName))
            {
                return skuCatalog.CloudEnvironmentSkus[skuName].DisplayName;
            }

            return skuName;
        }
    }
}
