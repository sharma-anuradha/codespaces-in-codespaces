// <copyright file="SystemConfigurationRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common
{
    /// <summary>
    /// A System configuration repository for backend
    /// </summary>
    public class SystemConfigurationRepository : ICachedSystemConfigurationRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemConfigurationRepository"/> class.
        /// </summary>
        /// <param name="regionalSystemConfigurationRepository">Regional system configuration repository.</param>
        /// <param name="globalSystemConfigurationRepository">Global system configuration repository.</param>
        public SystemConfigurationRepository(
            IRegionalSystemConfigurationRepository regionalSystemConfigurationRepository,
            IGlobalSystemConfigurationRepository globalSystemConfigurationRepository,
            SystemConfigurationMigrationSettings migrationSettings)
        {
            // Move to using global configurations collection instead of regional if enabled via AppSetting.
            if (migrationSettings.UseGlobalConfigurationCollection)
            {
                // Switch to using the global/resources configuration collection.
                ConfigurationRepository = globalSystemConfigurationRepository;
            }
            else
            {
                // use regional configuration collection for backend. 
                ConfigurationRepository = regionalSystemConfigurationRepository;
            }    
        }

        public async Task<SystemConfigurationRecord> GetAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            return await ConfigurationRepository.GetAsync(key, logger);
        }

        public async Task RefreshCacheAsync(IDiagnosticsLogger logger)
        {
            await ConfigurationRepository.RefreshCacheAsync(logger);
        }

        private ICachedSystemConfigurationRepository ConfigurationRepository { get; }

        public async Task<SystemConfigurationRecord> CreateAsync(SystemConfigurationRecord document, IDiagnosticsLogger logger)
        {
            return await ConfigurationRepository.CreateAsync(document, logger);
        }

        public async Task<SystemConfigurationRecord> CreateOrUpdateAsync(SystemConfigurationRecord document, IDiagnosticsLogger logger)
        {
            return await ConfigurationRepository.CreateOrUpdateAsync(document, logger);
        }

        public async Task<bool> DeleteAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            return await ConfigurationRepository.DeleteAsync(key, logger);
        }

        public async Task ForEachAsync(Expression<Func<SystemConfigurationRecord, bool>> where, IDiagnosticsLogger logger, Func<SystemConfigurationRecord, IDiagnosticsLogger, Task> itemCallback, Func<IEnumerable<SystemConfigurationRecord>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            await ConfigurationRepository.ForEachAsync(where, logger, itemCallback, pageResultsCallback);
        }

        public async Task ForEachAsync<TR>(Func<IOrderedQueryable<SystemConfigurationRecord>, IQueryable<TR>> queryBuilder, IDiagnosticsLogger logger, Func<TR, IDiagnosticsLogger, Task> itemCallback, Func<IEnumerable<TR>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            await ConfigurationRepository.ForEachAsync(queryBuilder, logger, itemCallback, pageResultsCallback);
        }

        public async Task<IEnumerable<SystemConfigurationRecord>> GetWhereAsync(Expression<Func<SystemConfigurationRecord, bool>> where, IDiagnosticsLogger logger, Func<IEnumerable<SystemConfigurationRecord>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            return await ConfigurationRepository.GetWhereAsync(where, logger, pageResultsCallback);
        }

        public async Task<IEnumerable<TR>> QueryAsync<TR>(Func<IOrderedQueryable<SystemConfigurationRecord>, IQueryable<TR>> queryBuilder, IDiagnosticsLogger logger, Func<IEnumerable<TR>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            return await ConfigurationRepository.QueryAsync(queryBuilder, logger, pageResultsCallback);
        }

        public async Task<IEnumerable<TR>> QueryAsync<TR>(Func<IDocumentClient, Uri, FeedOptions, IDocumentQuery<TR>> queryBuilder, IDiagnosticsLogger logger, Func<IEnumerable<TR>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            return await ConfigurationRepository.QueryAsync(queryBuilder, logger, pageResultsCallback);
        }

        public async Task<SystemConfigurationRecord> UpdateAsync(SystemConfigurationRecord document, IDiagnosticsLogger logger)
        {
            return await ConfigurationRepository.UpdateAsync(document, logger);
        }
    }
}
