// <copyright file="ISystemConfigurationRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository
{
    /// <summary>
    /// Configuration Repository.
    /// </summary>
    public interface ISystemConfigurationRepository : IDocumentDbCollection<SystemConfigurationRecord>
    {
    }
}
