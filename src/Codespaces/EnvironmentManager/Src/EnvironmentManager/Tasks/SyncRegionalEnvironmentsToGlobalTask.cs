// <copyright file="SyncRegionalEnvironmentsToGlobalTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Re-synchronoize regional cloud environment records back to the global repository where they are broken in the global repo.
    /// </summary>
    public class SyncRegionalEnvironmentsToGlobalTask : EnvironmentTaskBase, ISyncRegionalEnvironmentsToGlobalTask
    {
        private static readonly TimeSpan QueryDelay = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncRegionalEnvironmentsToGlobalTask"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Target Environment Manager Settings.</param>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="configurationReader">Target configuration reader.</param>
        public SyncRegionalEnvironmentsToGlobalTask(
            EnvironmentManagerSettings environmentManagerSettings,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IControlPlaneInfo controlPlaneInfo,
            IConfigurationReader configurationReader)
            : base(environmentManagerSettings, cloudEnvironmentRepository, taskHelper, claimedDistributedLease, resourceNameBuilder, configurationReader)
        {
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
        }

        protected override string ConfigurationBaseName => "SyncRegionalEnvironmentsToGlobalTask";

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(SyncRegionalEnvironmentsToGlobalTask)}Lease");

        private string LogBaseName => EnvironmentLoggingConstants.SyncRegionalEnvironmentsToGlobalTask;

        private IControlPlaneInfo ControlPlaneInfo { get; }

        /// <inheritdoc/>
        protected override Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    // Basic shard by starting resource id character
                    // NOTE: If over time we needed an additional dimention, we could add region
                    //       and do a cross product with it.
                    var idShards = ScheduledTaskHelpers.GetIdShards();

                    // Run through found resources in the background
                    await TaskHelper.RunConcurrentEnumerableAsync(
                        $"{LogBaseName}_run_unit_check",
                        idShards,
                        (idShard, itemLogger) => CoreRunUnitAsync(idShard, itemLogger),
                        childLogger,
                        (idShard, itemLogger) => ObtainLeaseAsync($"{LeaseBaseName}-{idShard}", claimSpan, itemLogger));

                    return !Disposed;
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        private async Task CoreRunUnitAsync(string idShard, IDiagnosticsLogger logger)
        {
            logger.FluentAddValue("TaskEnvironmentIdShard", idShard);

            await CloudEnvironmentRepository.GlobalRepository.ForEachAsync(
                (x) => x.Id.StartsWith(idShard) && x.ControlPlaneLocation == ControlPlaneInfo.Stamp.Location && x.State == CloudEnvironmentState.None,
                logger.NewChildLogger(),
                CoreRunUnitAsync,
                (_, __) => Task.Delay(QueryDelay));
        }

        private Task CoreRunUnitAsync(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_migrate",
                async (childLogger) =>
                {
                    childLogger.AddEnvironmentId(environment.Id);

                    var regionalEnvironment = await CloudEnvironmentRepository.RegionalRepository.GetAsync(environment.Id, logger.NewChildLogger());

                    if (regionalEnvironment != null)
                    {
                        // Copy the valid property values from the regional environment back to the global environment.
                        CopyCloudEnvironment(regionalEnvironment, environment);

                        try
                        {
                            await CloudEnvironmentRepository.GlobalRepository.UpdateAsync(environment, logger.NewChildLogger());
                        }
                        catch (DocumentClientException ex)
                        {
                            // Note: If we get a Precondition Failed error, it means that the environment has been modified behind our
                            // backs which likely means that we don't need to re-sync it.
                            if (ex.StatusCode != System.Net.HttpStatusCode.PreconditionFailed)
                            {
                                throw;
                            }
                        }
                    }
                },
                swallowException: true);
        }

        private void CopyCloudEnvironment(CloudEnvironment environment, CloudEnvironment target)
        {
            foreach (var property in environment.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                MethodInfo getter, setter;

                if ((getter = property.GetGetMethod(false)) == null ||
                    (setter = property.GetSetMethod(false)) == null)
                {
                    continue;
                }

                var value = getter.Invoke(environment, new object[0]);

                setter.Invoke(target, new object[1] { value });
            }
        }
    }
}
