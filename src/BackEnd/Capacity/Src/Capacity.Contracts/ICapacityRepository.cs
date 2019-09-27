// <copyright file="ICapacityRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Azure.Storage.DocumentDB;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// Represents a repository of <see cref="CapacityRecord"/>.
    /// </summary>
    public interface ICapacityRepository : IDocumentDbCollection<CapacityRecord>
    {
    }
}
