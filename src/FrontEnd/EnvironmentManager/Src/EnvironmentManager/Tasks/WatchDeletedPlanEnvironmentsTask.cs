// <copyright file="WatchDeletedPlanEnvironmentsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Cleans up all the environments from plans that have been deleted.
    /// </summary>
    public class WatchDeletedPlanEnvironmentsTask : EnvironmentTaskBase, IWatchDeletedPlanEnvironmentsTask
    {
        // Add an artificial delay between DB queries so that we reduce bursty load on our database to prevent throttling for end users
        private static readonly TimeSpan QueryDelay = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchDeletedPlanEnvironmentsTask"/> class.
        /// </summary>
        /// <param name="planRepository">The plan repository used to get deleted plans.</param>
        /// <param name="environmentManagerSettings">Target Environment Manager Settings.</param>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="envManager">the environment manager needed to delete environments.</param>
        /// <param name="controlPlaneInfo"> The control plane info used to figure out locations to run from.</param>
        public WatchDeletedPlanEnvironmentsTask(
            IPlanRepository planRepository,
            EnvironmentManagerSettings environmentManagerSettings,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IEnvironmentManager envManager,
            IControlPlaneInfo controlPlaneInfo)
             : base(environmentManagerSettings, cloudEnvironmentRepository, taskHelper, claimedDistributedLease, resourceNameBuilder)
        {
            PlanRepository = planRepository;
            EnvManager = envManager;
            ControlPlaneInfo = controlPlaneInfo;
        }

        private IPlanRepository PlanRepository { get; }

        private IEnvironmentManager EnvManager { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchDeletedPlanEnvironmentsTask)}Lease");

        private string LogBaseName => EnvironmentLoggingConstants.WatchDeletedPlanEnvironmentsTask;

        /// <inheritdoc/>
        public Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    var locations = ControlPlaneInfo.GetAllDataPlaneLocations();

                    // Run through found plans in the background
                    await TaskHelper.RunConcurrentEnumerableAsync(
                        $"{LogBaseName}_run_unit_check",
                        locations,
                        (location, itemLogger) => CoreRunUnitAsync(location, itemLogger),
                        childLogger.NewChildLogger(),
                        (location, itemLogger) => ObtainLeaseAsync($"{LeaseBaseName}-{location}", claimSpan, itemLogger));

                    return !Disposed;
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        private Task CoreRunUnitAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            return PlanRepository.ForEachAsync(
                x => x.IsDeleted == true &&
                     x.Plan.Location == location,
                logger.NewChildLogger(),
                (plan, innerLogger) => CoreDeletePlanEnvironmentsAsync(plan, innerLogger),
                (_, __) => Task.Delay(QueryDelay));

            // TODO: Let's flag the plan as done if it's been done to avoid reprocessing.
        }

        private async Task CoreDeletePlanEnvironmentsAsync(VsoPlan plan, IDiagnosticsLogger innerLogger)
        {
            await innerLogger.OperationScopeAsync(
                $"{LogBaseName}_delete_plan_environments",
                async (childLogger)
                =>
                {
                    childLogger.AddVsoPlan(plan.Plan);

                    var environments = await EnvManager.ListAsync(
                        childLogger.NewChildLogger(), planId: plan.Plan.ResourceId);
                    var nonDeletedEnvironments = environments.Where(t => t.State != CloudEnvironmentState.Deleted).ToList();
                    if (nonDeletedEnvironments.Any())
                    {
                        var deletedEnvironmentCount = 0;
                        foreach (var environment in nonDeletedEnvironments)
                        {
                            childLogger.AddCloudEnvironment(environment);
                            var result = await EnvManager.DeleteAsync(environment, childLogger.NewChildLogger());
                            if (result)
                            {
                                deletedEnvironmentCount++;
                            }
                            else
                            {
                                childLogger.LogError($"{LogBaseName}_delete_environment_error");
                            }
                        }

                        childLogger
                            .FluentAddValue($"DeletedEnvironmentSuccess", $"{nonDeletedEnvironments.Count() == deletedEnvironmentCount}");
                    }

                    // Pause to rate limit ourselves
                    await Task.Delay(QueryDelay);
                }, swallowException: true);
        }
    }
}
