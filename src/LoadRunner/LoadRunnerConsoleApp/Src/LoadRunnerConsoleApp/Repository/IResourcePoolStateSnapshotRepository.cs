// <copyright file="IResourcePoolStateSnapshotRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository
{
    /// <summary>
    /// Repository that fronts access to ResourceRecords.
    /// </summary>
    public interface IResourcePoolStateSnapshotRepository : IDocumentDbCollection<ResourcePoolStateSnapshotRecord>
    {
    }
}
