// <copyright file="IResourcePoolSettingsRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Azure.Storage.DocumentDB;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ScalingEngine
{
    /// <summary>
    /// Repository that fronts access to ResourceRecords.
    /// </summary>
    public interface IResourcePoolSettingsRepository : IDocumentDbCollection<ResourcePoolSettingsRecord>
    {
    }
}
