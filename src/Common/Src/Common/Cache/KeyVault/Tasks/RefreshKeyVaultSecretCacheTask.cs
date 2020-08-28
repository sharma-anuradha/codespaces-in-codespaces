// <copyright file="RefreshKeyVaultSecretCacheTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// A task that will recurringly generate telemetry that logs various state information about the CloudEnvironment repository.
    /// </summary>
    public class RefreshKeyVaultSecretCacheTask : BaseBackgroundTask, IRefreshKeyVaultSecretCacheTask
    {
        private const string LogBaseName = "refresh_key_vault_secret_cache";

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshKeyVaultSecretCacheTask"/> class.
        /// </summary>
        /// <param name="taskHelper">The Task helper.</param>
        /// <param name="caches">list of keyvalut caches that required refresh.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        public RefreshKeyVaultSecretCacheTask(
            ITaskHelper taskHelper,
            IEnumerable<IRefreshKeyVaultSecretCache> caches,
            IConfigurationReader configurationReader)
            : base(configurationReader)
        {
            TaskHelper = taskHelper;
            Caches = caches;
        }

        private ITaskHelper TaskHelper { get; }

        private IEnumerable<IRefreshKeyVaultSecretCache> Caches { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public override void Dispose()
        {
            Disposed = true;
        }

        /// <inheritdoc/>
        protected override string ConfigurationBaseName => "RefreshKeyVaultSecretCacheTask";

        /// <inheritdoc/>
        protected override Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
               $"{LogBaseName}_run",
               (childLogger) =>
               {
                   TaskHelper.RunBackground(
                       $"{LogBaseName}_refresh_all",
                       (itemLogger) => CoreRunUnitAsync(itemLogger));

                   return Task.FromResult(!Disposed);
               },
               (e, _) => Task.FromResult(!Disposed),
               swallowException: true);
        }

        private async Task CoreRunUnitAsync(IDiagnosticsLogger itemLogger)
        {
            foreach (var cache in Caches)
            {
                await cache.RefreshAllSecretsAsync(itemLogger.NewChildLogger());
            }
        }
    }
}