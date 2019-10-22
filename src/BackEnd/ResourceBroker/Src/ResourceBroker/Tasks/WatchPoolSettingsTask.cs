// <copyright file="WatchPoolSettingsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Task mananager that regularly checks if pool settings have been updated.
    /// </summary>
    public class WatchPoolSettingsTask : IWatchPoolSettingsTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchPoolSettingsTask"/> class.
        /// </summary>
        /// <param name="resourcePoolSettingsRepository">Target resource pool settings repository.</param>
        /// <param name="resourcePoolDefinitionStore">Target resource pool definition store.</param>
        public WatchPoolSettingsTask(
            IResourcePoolSettingsRepository resourcePoolSettingsRepository,
            IResourcePoolDefinitionStore resourcePoolDefinitionStore)
        {
            ResourcePoolSettingsRepository = resourcePoolSettingsRepository;
            ResourcePoolDefinitionStore = resourcePoolDefinitionStore;
        }

        private IResourcePoolSettingsRepository ResourcePoolSettingsRepository { get; }

        private IResourcePoolDefinitionStore ResourcePoolDefinitionStore { get; }

        private bool Disposed { get; set; }

        private string LogBaseName => ResourceLoggingConstants.WatchPoolSettingsTask;

        /// <inheritdoc/>
        public Task<bool> RunAsync(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    // Pull out core settings records
                    var settings = (await ResourcePoolSettingsRepository.GetWhereAsync(x => true, childLogger.NewChildLogger()))
                        .ToDictionary(x => x.Id);

                    // Run through each pool item
                    var resourceUnits = await ResourcePoolDefinitionStore.RetrieveDefinitions();
                    foreach (var resourceUnit in resourceUnits)
                    {
                        // Overrides the value if we have settings for it
                        if (settings.TryGetValue(resourceUnit.Details.GetPoolDefinition(), out var resourceSetting))
                        {
                            resourceUnit.OverrideTargetCount = resourceSetting.TargetCount;
                            resourceUnit.OverrideIsEnabled = resourceSetting.IsEnabled;
                        }
                        else
                        {
                            // Clear out any overrides if we don't have matches
                            resourceUnit.OverrideTargetCount = null;
                            resourceUnit.OverrideIsEnabled = null;
                        }
                    }

                    return !Disposed;
                },
                (e) => !Disposed);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }
    }
}
