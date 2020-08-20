// <copyright file="MockRegionalCloudEnvironmentRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository.Mocks
{
    /// <summary>
    /// A mock cloud environment repository.
    /// </summary>
    public class MockRegionalCloudEnvironmentRepository : MockGlobalCloudEnvironmentRepository, IRegionalCloudEnvironmentRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockRegionalCloudEnvironmentRepository"/> class.
        /// </summary>
        /// <param name="location">The mock control-plane location.</param>
        public MockRegionalCloudEnvironmentRepository(AzureLocation location = AzureLocation.WestUs2)
            : base(location)
        {
        }
    }
}
