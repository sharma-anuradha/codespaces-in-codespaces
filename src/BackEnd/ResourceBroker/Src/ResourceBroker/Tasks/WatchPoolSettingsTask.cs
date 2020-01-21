// <copyright file="WatchPoolSettingsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Newtonsoft.Json;

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

                    childLogger.FluentAddValue("SettingsFoundCount", settings.Count)
                        .FluentAddValue("SettingsFoundData", JsonConvert.SerializeObject(
                            settings, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto }));

                    // Fetch current pool items
                    var resourceUnits = await ResourcePoolDefinitionStore.RetrieveDefinitions();

                    childLogger.FluentAddValue("SettingsFoundPoolDefinitionCount", resourceUnits.Count());

                    // Run through each pool item
                    foreach (var resourceUnit in resourceUnits)
                    {
                        _ = childLogger.OperationScopeAsync(
                            $"{LogBaseName}_run_unit_check",
                            (itemLogger) =>
                            {
                                var poolDefinition = resourceUnit.Details.GetPoolDefinition();

                                itemLogger.FluentAddValue("SettingsFoundPool", poolDefinition)
                                    .FluentAddValue("SettingPreOverrideTargetCount", resourceUnit.OverrideTargetCount)
                                    .FluentAddValue("SettingPreOverrideIsEnabled", resourceUnit.OverrideIsEnabled);

                                // Overrides the value if we have settings for it
                                if (settings.TryGetValue(poolDefinition, out var resourceSetting))
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

                                itemLogger.FluentAddValue("SettingPostOverrideTargetCount", resourceUnit.OverrideTargetCount)
                                    .FluentAddValue("SettingPostOverrideIsEnabled", resourceUnit.OverrideIsEnabled);

                                return Task.CompletedTask;
                            });
                    }

                    return !Disposed;
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }
    }
}
