// <copyright file="NullSystemCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <inheritdoc/>
    public class NullSystemCatalog : ISystemCatalog
    {
        /// <inheritdoc/>
        public IAzureSubscriptionCatalog AzureSubscriptionCatalog => throw new NotSupportedException();

        /// <inheritdoc/>
        public ISkuCatalog SkuCatalog => throw new NotSupportedException();

        /// <inheritdoc/>
        public IPlanSkuCatalog PlanSkuCatalog => throw new NotImplementedException();
    }
}
