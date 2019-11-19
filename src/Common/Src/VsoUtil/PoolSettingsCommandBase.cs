// <copyright file="PoolSettingsCommandBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// Base class for pool settings that take a --pool-id parameter.
    /// </summary>
    public abstract class PoolSettingsCommandBase : CommandBase
    {
        private const string PoolId = "pool-id";

        /// <summary>
        /// Gets or sets the pool id.
        /// </summary>
        [Option('i', PoolId, Required = true, HelpText = "The pool id.")]
        public string Id { get; set; }

        /// <summary>
        /// Gets a settings record from the repository.
        /// </summary>
        /// <param name="services">The service container.</param>
        /// <returns>The settings record.</returns>
        protected async Task<ResourcePoolSettingsRecord> GetResourcePoolSettingsRecordAsync(IServiceProvider services)
        {
            var settingsRepo = services.GetRequiredService<IResourcePoolSettingsRepository>();
            var loggerFactory = services.GetRequiredService<IDiagnosticsLoggerFactory>();
            var logger = loggerFactory.New();
            var settings = await settingsRepo.GetAsync(Id, logger);
            return settings;
        }

        /// <summary>
        /// Gets a resource pool snapshot record from the repository.
        /// </summary>
        /// <param name="services">The service container.</param>
        /// <returns>The pool snapshot record.</returns>
        protected async Task<ResourcePoolStateSnapshotRecord> GetResourcePoolSnapshotAsync(IServiceProvider services)
        {
            var snapshotRepository = services.GetRequiredService<IResourcePoolStateSnapshotRepository>();
            var loggerFactory = services.GetRequiredService<IDiagnosticsLoggerFactory>();
            var logger = loggerFactory.New();
            var snapshot = await snapshotRepository.GetAsync(Id, logger);
            return snapshot;
        }

        /// <summary>
        /// Create or update the settings record.
        /// </summary>
        /// <param name="services">The service container.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>The updated settings.</returns>
        protected async Task<ResourcePoolSettingsRecord> CreateOrUpdateResourcePoolSettingsRecordAsync(IServiceProvider services, ResourcePoolSettingsRecord settings)
        {
            var settingsRepo = services.GetRequiredService<IResourcePoolSettingsRepository>();
            var loggerFactory = services.GetRequiredService<IDiagnosticsLoggerFactory>();
            var logger = loggerFactory.New();
            settings.Id = Id;
            settings = await settingsRepo.CreateOrUpdateAsync(settings, logger);
            return settings;
        }

        /// <summary>
        /// Delete the settings record.
        /// </summary>
        /// <param name="services">The service container.</param>
        /// <returns>True if deleted.</returns>
        protected async Task<bool> DeleteResourcePoolSettingsRecordAsync(IServiceProvider services)
        {
            var settingsRepo = services.GetRequiredService<IResourcePoolSettingsRepository>();
            var loggerFactory = services.GetRequiredService<IDiagnosticsLoggerFactory>();
            var logger = loggerFactory.New();
            var result = await settingsRepo.DeleteAsync(Id, logger);
            return result;
        }
    }
}
