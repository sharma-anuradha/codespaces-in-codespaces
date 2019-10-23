// <copyright file="MockAzureSubscriptionCapacityProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Mocks
{
    /// <summary>
    /// The mock capacity manager.
    /// </summary>
    public class MockAzureSubscriptionCapacityProvider : IAzureSubscriptionCapacityProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockAzureSubscriptionCapacityProvider"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCatalog">The azure subscription catalog.</param>
        public MockAzureSubscriptionCapacityProvider()
        {
        }

        /// <inheritdoc/>
        public Task<IEnumerable<AzureResourceUsage>> GetAzureResourceUsageAsync(IAzureSubscription subscription, AzureLocation location, ServiceType serviceType, IDiagnosticsLogger logger)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<AzureResourceUsage>> LoadAzureResourceUsageAsync(IAzureSubscription subscription, AzureLocation location, ServiceType serviceType, IDiagnosticsLogger logger)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpdateAzureResourceUsageAsync(IAzureSubscription subscription, AzureLocation location, ServiceType serviceType, IDiagnosticsLogger logger)
        {
            throw new System.NotImplementedException();
        }
    }
}
