// <copyright file="WatchEnvironmentsToBeUpdatedTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Watch Windows Environments to be Updated task.
    /// </summary>
    public class WatchEnvironmentsToBeUpdatedTask : EnvironmentTaskBase, IWatchEnvironmentsToBeUpdatedTask
    {
        // Add an artificial delay between DB queries so that we reduce bursty load on our database to prevent throttling for end users
        private static readonly TimeSpan QueryDelay = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchEnvironmentsToBeUpdatedTask"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Target Environment Manager Settings.</param>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="environmentContinuationOperations">Target Environment Continuation Operations.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        /// <param name="skuCatalog">Sku Catalog.</param>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="controlPlaneInfo">Control Plane info.</param>
        /// <param name="serviceUriBuilder">Service URI builder.</param>
        /// <param name="currentIdentityProvider">Current identity provider.</param>
        /// <param name="superuserIdentity">Super user identity.</param>
        public WatchEnvironmentsToBeUpdatedTask(
            EnvironmentManagerSettings environmentManagerSettings,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IEnvironmentContinuationOperations environmentContinuationOperations,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IConfigurationReader configurationReader,
            ISkuCatalog skuCatalog,
            IServiceProvider serviceProvider,
            IControlPlaneInfo controlPlaneInfo,
            IServiceUriBuilder serviceUriBuilder,
            ICurrentIdentityProvider currentIdentityProvider,
            VsoSuperuserClaimsIdentity superuserIdentity)
            : base(environmentManagerSettings, cloudEnvironmentRepository, taskHelper, claimedDistributedLease, resourceNameBuilder, configurationReader)
        {
            EnvironmentContinuationOperations = environmentContinuationOperations;
            SkuCatalog = skuCatalog;
            ServiceProvider = serviceProvider;
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            ServiceUriBuilder = Requires.NotNull(serviceUriBuilder, nameof(serviceUriBuilder));
            CurrentIdentityProvider = Requires.NotNull(currentIdentityProvider, nameof(currentIdentityProvider));
            SuperuserIdentity = Requires.NotNull(superuserIdentity, nameof(superuserIdentity));
        }

        /// <inheritdoc/>
        protected override string ConfigurationBaseName => nameof(WatchEnvironmentsToBeUpdatedTask);

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchEnvironmentsToBeUpdatedTask)}Lease");

        private string LogBaseName => EnvironmentLoggingConstants.WatchEnvironmentsToBeUpdatedTask;

        private IEnvironmentContinuationOperations EnvironmentContinuationOperations { get; }

        private ISkuCatalog SkuCatalog { get; }

        private IServiceProvider ServiceProvider { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IServiceUriBuilder ServiceUriBuilder { get; }

        private ICurrentIdentityProvider CurrentIdentityProvider { get; }

        private VsoSuperuserClaimsIdentity SuperuserIdentity { get; }

        /// <inheritdoc/>
        protected override Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    var idShards = ScheduledTaskHelpers.GetIdShards();
                    var maxJobCount = await EnvironmentManagerSettings.EnvironmentUpdateMaxActiveCount(childLogger);

                    childLogger.FluentAddValue("TaskEnvironmentMaxActiveCount", maxJobCount);

                    // Run through found resources in the background
                    await TaskHelper.RunConcurrentEnumerableAsync(
                        $"{LogBaseName}_run_unit_check",
                        idShards,
                        (idShard, itemLogger) => CoreRunUnitAsync(idShard, maxJobCount, itemLogger),
                        childLogger,
                        (idShard, itemLogger) => ObtainLeaseAsync($"{LeaseBaseName}-{idShard}", claimSpan, itemLogger));

                    return !Disposed;
                });
        }

        private async Task CoreRunUnitAsync(string idShard, int maxJobCount, IDiagnosticsLogger logger)
        {
            logger.FluentAddValue("TaskEnvironmentIdShard", idShard);

            // Check to see how many jobs are currently running
            var activeCount = await CloudEnvironmentRepository.GetEnvironmentUpdateJobActiveCountAsync(logger.NewChildLogger());

            logger.FluentAddValue("TaskEnvironmentActiveCount", activeCount);

            if (activeCount <= maxJobCount)
            {
                // Get environments needing an update
                var records = await CloudEnvironmentRepository.GetEnvironmentsToBeUpdatedAsync(
                    idShard,
                    "Windows",
                    logger.NewChildLogger());

                logger.FluentAddValue("TaskFoundItems", records.Count());

                // Run through each found item
                foreach (var record in records)
                {
                    await CoreRunUnitAsync(record, logger);
                }
            }
        }

        private AzureLocation GetResourceLocation(CloudEnvironment environment)
        {
            if (environment.Compute != default)
            {
                return environment.Compute.Location;
            }
            else if (environment.OSDisk != default)
            {
                return environment.OSDisk.Location;
            }
            else if (environment.Storage != default)
            {
                return environment.Storage.Location;
            }
            else if (environment.OSDiskSnapshot != default)
            {
                return environment.OSDiskSnapshot.Location;
            }

            // If we can't find a location, something's wrong with the record?
            return AzureLocation.WestUs2;
        }

        private bool ShouldUpdate(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            if (!SkuCatalog.CloudEnvironmentSkus.TryGetValue(environment.SkuName, out var sku))
            {
                // We don't know about this SKU, so ignore update
                return false;
            }

            // Check time for the location the environment is in
            var resourceLocation = GetResourceLocation(environment);
            var currentTimeInLocation = resourceLocation.GetTimeInLocation(DateTime.UtcNow);

            logger.FluentAddBaseValue("LocalTime", currentTimeInLocation.ToString())
                .FluentAddBaseValue("Location", resourceLocation);

            if (currentTimeInLocation.Hour > 6 || currentTimeInLocation.Hour < 2)
            {
                // Only attempt updating VMs between 2AM and 6AM on their location.
                // Special care has to be taken on the up and low limits, as 2AM in
                // SouthEastAsia could be -3 (or more) in other Asia regions.
                return false;
            }

            if (environment.SystemStatusInfo != default && environment.SystemStatusInfo.VsVersion != default)
            {
                if (Version.TryParse(sku.ComputeImage.VsVersion, out var skuVersion))
                {
                    return skuVersion > environment.SystemStatusInfo.VsVersion;
                }
            }

            return true;
        }

        private Task CoreRunUnitAsync(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_update",
                async (childLogger) =>
                {
                    childLogger.AddCloudEnvironment(environment)
                        .FluentAddBaseValue("SkuName", environment.SkuName)
                        .FluentAddBaseValue("UpdateAttemptCount", environment.Transitions.Updating.AttemptCount);

                    if (ShouldUpdate(environment, logger))
                    {
                        var cloudEnvironmentParams = GetCloudEnvironmentParameters(environment);

                        var environmentManager = ServiceProvider.GetRequiredService<IEnvironmentManager>();

                        using (CurrentIdentityProvider.SetScopedIdentity(SuperuserIdentity))
                        {
                            await environmentManager.UpdateSystemAsync(
                                Guid.Parse(environment.Id),
                                cloudEnvironmentParams,
                                logger.NewChildLogger());
                        }
                    }

                    // Pause to rate limit ourselves
                    await Task.Delay(QueryDelay);
                },
                swallowException: true);
        }

        private CloudEnvironmentParameters GetCloudEnvironmentParameters(CloudEnvironment environment)
        {
            var currentStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(environment.Location);
            var currentUserProvider = ServiceProvider.GetService<ICurrentUserProvider>();

            // FIXME: is there a better way to retrieve this?
            var frontendUri = ServiceUriBuilder.GetServiceUri($"http://{currentStamp.DnsHostName}/api/v1/environments/", currentStamp);
            var callbackUriFormat = ServiceUriBuilder.GetCallbackUriFormat(frontendUri.ToString(), currentStamp);

            return new UpdateCloudEnvironmentParameters
            {
                FrontEndServiceUri = frontendUri,
                CallbackUriFormat = callbackUriFormat.ToString(),
                UserAuthToken = currentUserProvider.BearerToken,
                CurrentUserIdSet = currentUserProvider.CurrentUserIdSet,
            };
        }
    }
}
