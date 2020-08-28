// <copyright file="WatchEnvironmentsToBeHardDeleteTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Watch Soft Deleted Environments Task to be hard deleted. .
    /// </summary>
    public class WatchEnvironmentsToBeHardDeleteTask : EnvironmentTaskBase, IWatchSoftDeletedEnvironmentToBeHardDeletedTask
    {
        // Add an artificial delay between DB queries so that we reduce bursty load on our database to prevent throttling for end users
        private static readonly TimeSpan QueryDelay = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchEnvironmentsToBeHardDeleteTask"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Target Environment Manager Settings.</param>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="environmentContinuationOperations">Target Resource Broker Http Client.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="environmentManager">Target Environment Manager.</param>
        /// <param name="currentIdentityProvider">Target identity provider.</param>
        /// <param name="superuserIdentity">Target super user identity.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        public WatchEnvironmentsToBeHardDeleteTask(
            EnvironmentManagerSettings environmentManagerSettings,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IEnvironmentContinuationOperations environmentContinuationOperations,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IEnvironmentManager environmentManager,
            ICurrentIdentityProvider currentIdentityProvider,
            VsoSuperuserClaimsIdentity superuserIdentity,
            IConfigurationReader configurationReader)
            : base(environmentManagerSettings, cloudEnvironmentRepository, taskHelper, claimedDistributedLease, resourceNameBuilder, configurationReader)
        {
            EnvironmentContinuationOperations = environmentContinuationOperations;
            EnvironmentManager = environmentManager;
            CurrentIdentityProvider = currentIdentityProvider;
            SuperuserIdentity = superuserIdentity;
        }

        /// <inheritdoc/>
        protected override string ConfigurationBaseName => "WatchEnvironmentsToBeHardDeleteTask";

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchEnvironmentsToBeHardDeleteTask)}Lease");

        private string LogBaseName => EnvironmentLoggingConstants.WatchSoftDeletedEnvironmentToBeHardDeletedTask;

        private IEnvironmentContinuationOperations EnvironmentContinuationOperations { get; }

        private IEnvironmentManager EnvironmentManager { get; }

        private ICurrentIdentityProvider CurrentIdentityProvider { get; }

        private VsoSuperuserClaimsIdentity SuperuserIdentity { get; }

        /// <inheritdoc/>
        protected override Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    using (CurrentIdentityProvider.SetScopedIdentity(SuperuserIdentity))
                    {
                        // Bail if disabled
                        var isEnabled = await EnvironmentManagerSettings.EnvironmentSoftDeleteEnabled(childLogger);
                        childLogger.FluentAddValue("TaskIsEnabled", isEnabled);
                        if (!isEnabled)
                        {
                            return !Disposed;
                        }

                        // Settings for query
                        var cutoffHours = await EnvironmentManagerSettings.EnvironmentHardDeleteCutoffHours(childLogger);
                        var cutoffTime = DateTime.UtcNow.AddHours(cutoffHours * -1);

                        childLogger.FluentAddValue("TaskEnvironmentCutoffHours", cutoffHours)
                            .FluentAddValue("TaskEnvironmentCutoffTime", cutoffTime);

                        var idShards = ScheduledTaskHelpers.GetIdShards();

                        // Run through found resources in the background
                        await TaskHelper.RunConcurrentEnumerableAsync(
                            $"{LogBaseName}_run_unit_check",
                            idShards,
                            (idShard, itemLogger) => CoreRunUnitAsync(idShard, cutoffTime, itemLogger),
                            childLogger,
                            (idShard, itemLogger) => ObtainLeaseAsync($"{LeaseBaseName}-{idShard}", claimSpan, itemLogger));

                        return !Disposed;
                    }
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        private async Task CoreRunUnitAsync(string idShard, DateTime cutoffTime, IDiagnosticsLogger logger)
        {
            logger.FluentAddValue("TaskEnvironmentIdShard", idShard);

            // Get environments to be deleted permanently.
            var records = await CloudEnvironmentRepository.GetEnvironmentsReadyForHardDeleteAsync(
                idShard, cutoffTime, logger.NewChildLogger());

            logger.FluentAddValue("TaskFoundItems", records.Count());

            // Run through each found item
            foreach (var record in records)
            {
                await CoreRunUnitAsync(record, logger);
            }
        }

        private Task CoreRunUnitAsync(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_hard_delete",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("EnvironmentId", environment.Id);

                    // Hard delete the environment.
                    await EnvironmentManager.HardDeleteAsync(Guid.Parse(environment.Id), childLogger.NewChildLogger());

                    // Pause to rate limit ourselves
                    await Task.Delay(QueryDelay);
                },
                swallowException: true);
        }
    }
}
