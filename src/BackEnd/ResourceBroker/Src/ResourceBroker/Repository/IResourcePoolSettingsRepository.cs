// <copyright file="IResourcePoolSettingsRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository
{
    /// <summary>
    /// Repository that fronts access to ResourceRecords.
    /// </summary>
    public interface IResourcePoolSettingsRepository : IDocumentDbCollection<ResourcePoolSettingsRecord>
    {
    }
}
