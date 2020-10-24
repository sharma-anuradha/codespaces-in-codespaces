// <copyright file="WatchDeletedPlanSecretStoresTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Cleans up all of the secret stores from plans that have been deleted.
    /// </summary>
    public class WatchDeletedPlanSecretStoresTask : EnvironmentTaskBase, IWatchDeletedPlanSecretStoresTask
    {
        // Add an artificial delay between DB queries so that we reduce bursty load on our database to prevent throttling for end users
        private static readonly TimeSpan QueryDelay = TimeSpan.FromMilliseconds(250);

        // The number of days since a plan has been deleted
        private static readonly int DaysSinceDeletion = -7;

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchDeletedPlanSecretStoresTask"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Target Environment Manager Settings.</param>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="currentIdentityProvider">Target identity provider.</param>
        /// <param name="planRepository">The plan repository used to get deleted plans.</param>
        /// <param name="secretStoreRepository">The secret store repository.</param>
        /// <param name="secretStoreManager">The secret store manager.</param>
        /// <param name="superuserIdentity">Target super user identity.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        /// <param name="jobSchedulerFeatureFlags">job queue feature flag</param>
        public WatchDeletedPlanSecretStoresTask(
            EnvironmentManagerSettings environmentManagerSettings,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IControlPlaneInfo controlPlaneInfo,
            ICurrentIdentityProvider currentIdentityProvider,
            VsoSuperuserClaimsIdentity superuserIdentity,
            IPlanRepository planRepository,
            ISecretStoreRepository secretStoreRepository,
            ISecretStoreManager secretStoreManager,
            IJobSchedulerFeatureFlags jobSchedulerFeatureFlags,
            IConfigurationReader configurationReader)
            : base(environmentManagerSettings, cloudEnvironmentRepository, taskHelper, claimedDistributedLease, resourceNameBuilder, jobSchedulerFeatureFlags, configurationReader)
        {
            ControlPlaneInfo = controlPlaneInfo;
            CurrentIdentityProvider = currentIdentityProvider;
            SuperuserIdentity = superuserIdentity;
            PlanRepository = planRepository;
            SecretStoreRepository = secretStoreRepository;
            SecretStoreManager = secretStoreManager;
        }

        /// <inheritdoc/>
        protected override string ConfigurationBaseName => "WatchDeletedPlanSecretStoresTask";

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchDeletedPlanSecretStoresTask)}Lease");

        private string LogBaseName => EnvironmentLoggingConstants.WatchDeletedPlanSecretStoresTask;

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private ICurrentIdentityProvider CurrentIdentityProvider { get; }

        private IPlanRepository PlanRepository { get; }

        private ISecretStoreRepository SecretStoreRepository { get; }

        private ISecretStoreManager SecretStoreManager { get; }

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
                        var locations = ControlPlaneInfo.Stamp.DataPlaneLocations;

                        // Run through found resources in the background
                        await TaskHelper.RunConcurrentEnumerableAsync(
                            $"{LogBaseName}_run_unit_check",
                            locations,
                            (location, itemLogger) => CoreRunUnitAsync(location, itemLogger),
                            childLogger,
                            (location, itemLogger) => ObtainLeaseAsync($"{LeaseBaseName}-{location}", claimSpan, itemLogger));

                        return !Disposed;
                    }
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        private Task CoreRunUnitAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            return PlanRepository.ForEachAsync(
                x => (!x.AreSecretStoresDeleted.HasValue ||
                     x.AreSecretStoresDeleted != true) &&
                     x.IsDeleted == true &&
                     (x.DeletedDate <= DateTime.UtcNow.AddDays(DaysSinceDeletion) ||
                     x.DeletedDate == null) &&
                     x.Plan.Location == location,
                logger.NewChildLogger(),
                (plan, innerLogger) => CoreDeletePlanSecretStoresAsync(plan, innerLogger),
                (_, __) => Task.Delay(QueryDelay));
        }

        private async Task CoreDeletePlanSecretStoresAsync(VsoPlan plan, IDiagnosticsLogger innerLogger)
        {
            await innerLogger.OperationScopeAsync(
                $"{LogBaseName}_delete_plan_secret_store",
                async (childLogger)
                =>
                {
                    childLogger.AddVsoPlan(plan);

                    // Default to true so even in the case there aren't any secret stores, this plan won't be reprocessed
                    var deletionsSuccessful = true;
                    var planSecretStores = await SecretStoreRepository.GetSecretStoresByPlanIdAsync(plan.Plan.ResourceId, childLogger.NewChildLogger());
                    if (planSecretStores.Any())
                    {
                        var deletedSecretStoreCount = 0;
                        foreach (var secretStore in planSecretStores)
                        {
                            var result = await SecretStoreManager.DeleteSecretStoreAsync(
                                plan.Plan.ResourceId,
                                secretStore.Id,
                                secretStore.SecretResource.ResourceId,
                                childLogger.NewChildLogger());

                            if (result)
                            {
                                deletedSecretStoreCount++;
                            }
                            else
                            {
                                deletionsSuccessful = false;
                                childLogger.LogError($"{LogBaseName}_delete_secret_store_error");
                            }
                        }

                        childLogger
                            .FluentAddValue($"DeletedSecretStoreSuccess", $"{planSecretStores.Count() == deletedSecretStoreCount}");
                    }

                    if (deletionsSuccessful)
                    {
                        plan.AreSecretStoresDeleted = true;
                        await PlanRepository.UpdateAsync(plan, childLogger);
                    }

                    // Pause to rate limit ourselves
                    await Task.Delay(QueryDelay);
                }, swallowException: true);
        }
    }
}
