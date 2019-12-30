// <copyright file="MockCapacityRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Mocks
{
    /// <summary>
    /// Mock implementation of <see cref"ICapacityRepository"/> for local development.
    /// </summary>
    public class MockCapacityRepository : MockRepository<CapacityRecord>, ICapacityRepository
    {
    }
}
