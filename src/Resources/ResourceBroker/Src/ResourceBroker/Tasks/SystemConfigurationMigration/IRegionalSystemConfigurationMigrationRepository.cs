// <copyright file="IRegionalSystemConfigurationMigrationRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Azure.Storage.DocumentDB;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks.SystemConfigurationMigration
{
    /// <summary>
    /// Interface for system configuration migration repository
    /// </summary>
    public interface IRegionalSystemConfigurationMigrationRepository : IDocumentDbCollection<SystemConfigurationMigrationRecord>
    {
    }
}