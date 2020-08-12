// <copyright file="LogSubscriptionStatisticsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
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
    /// Logs information about subscriptions and plans from various collections.
    /// </summary>
    public class LogSubscriptionStatisticsTask : EnvironmentTaskBase, ILogSubscriptionStatisticsTask, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogSubscriptionStatisticsTask"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Environment settings used to generate the lease name.</param>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="planRepository">the plan repository used for some queries.</param>
        /// <param name="taskHelper">The Task helper.</param>
        /// <param name="claimedDistributedLease">Used to get a lease for the duration of the telemetry.</param>
        /// <param name="resourceNameBuilder">Used to build the lease name.</param>
        public LogSubscriptionStatisticsTask(
            EnvironmentManagerSettings environmentManagerSettings,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IPlanRepository planRepository,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder)
            : base(environmentManagerSettings, cloudEnvironmentRepository, taskHelper, claimedDistributedLease, resourceNameBuilder)
        {
            PlanRepository = Requires.NotNull(planRepository, nameof(planRepository));
        }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(LogSubscriptionStatisticsTask)}Lease");

        private string LogBaseName => EnvironmentLoggingConstants.LogSubscriptionStatisticsTask;

        private IPlanRepository PlanRepository { get; }

        /// <inheritdoc />
        public Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
               $"{LogBaseName}_run",
               async (childLogger) =>
               {
                   using (var lease = await ObtainLeaseAsync(LeaseBaseName, claimSpan, childLogger))
                   {
                       childLogger.FluentAddValue("LeaseNotFound", lease == null);

                       if (lease != null)
                       {
                           await childLogger.OperationScopeAsync(
                                LogBaseName,
                                (itemLogger) => RunLogTaskAsync(itemLogger));
                       }
                   }

                   return !Disposed;
               },
               (e, _) => Task.FromResult(!Disposed),
               swallowException: true);
        }

        private async Task RunLogTaskAsync(IDiagnosticsLogger itemLogger)
        {
            var activeEnvironmentPlanCount = await CloudEnvironmentRepository.GetCloudEnvironmentPlanCountAsync(itemLogger);
            var activeEnvironmentSubscriptionCount = await CloudEnvironmentRepository.GetCloudEnvironmentSubscriptionCountAsync(itemLogger);
            var uniqueSubscriptionCount = await PlanRepository.GetPlanSubscriptionCountAsync(itemLogger);

            itemLogger.FluentAddValue($"UniqueSubscriptionCount", uniqueSubscriptionCount)
                          .FluentAddValue($"UniqueSubscriptionWithEnvCount", activeEnvironmentSubscriptionCount)
                          .FluentAddValue($"UniquePlansWithEnvCount", activeEnvironmentPlanCount)
                          .LogInfo("subscription_environment_measure");
        }
    }
}
