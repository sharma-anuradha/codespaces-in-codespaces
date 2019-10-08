// <copyright file="WatchPoolSettingsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;

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
        /// <param name="resourcePoolSettingsHandler">Target resource pool settings handler.</param>
        public WatchPoolSettingsTask(
            IResourcePoolSettingsRepository resourcePoolSettingsRepository,
            IResourcePoolSettingsHandler resourcePoolSettingsHandler)
        {
            ResourcePoolSettingsRepository = resourcePoolSettingsRepository;
            ResourcePoolSettingsHandler = resourcePoolSettingsHandler;
        }

        private IResourcePoolSettingsRepository ResourcePoolSettingsRepository { get; }

        private IResourcePoolSettingsHandler ResourcePoolSettingsHandler { get; }

        private bool Disposed { get; set; }

        private string LogBaseName => ResourceLoggingConstants.WatchPoolSettingsTask;

        /// <inheritdoc/>
        public Task<bool> RunAsync(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    // Pull out core records
                    var records = await ResourcePoolSettingsRepository.GetWhereAsync(x => true, childLogger.NewChildLogger());

                    // Pull out settings
                    var isEnabledSettings = records.ToDictionary(x => x.Id, x => x.IsEnabled);

                    // Update pool settings
                    await ResourcePoolSettingsHandler.UpdateResourceEnabledStateAsync(isEnabledSettings);

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
