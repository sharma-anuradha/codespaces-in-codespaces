// <copyright file="NullSystemCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi
{
    /// <inheritdoc/>
    public class NullSystemCatalog : ISystemCatalog
    {
        /// <inheritdoc/>
        public IAzureSubscriptionCatalog AzureSubscriptionCatalog => throw new System.NotImplementedException();

        /// <inheritdoc/>
        public ISkuCatalog SkuCatalog => throw new System.NotImplementedException();
    }
}
