// <copyright file="LogSubscriptionStatisticsJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Jobs
{
    /// <summary>
    /// Logs information about subscriptions and plans from various collections.
    /// </summary>
    public class LogSubscriptionStatisticsJobHandler : JobHandlerPayloadBase<LogSubscriptionStatisticsJobHandler.Payload>, IJobHandlerTarget
    {
        public const string LogBaseName = EnvironmentLoggingConstants.LogSubscriptionStatisticsTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogSubscriptionStatisticsJobHandler"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="planRepository">the plan repository used for some queries.</param>
        public LogSubscriptionStatisticsJobHandler(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IPlanRepository planRepository)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
            PlanRepository = Requires.NotNull(planRepository, nameof(planRepository));
        }

        public IJobHandler JobHandler => this;

        public string QueueId => EnvironmentJobQueueConstants.GenericQueueName;

        public AzureLocation? Location => null;

        private IPlanRepository PlanRepository { get; }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        protected override async Task HandleJobAsync(Payload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            await logger.OperationScopeAsync(
               $"{LogBaseName}_run",
               async (childLogger) =>
               {
                   await childLogger.OperationScopeAsync(
                        LogBaseName,
                        (itemLogger) => RunLogTaskAsync(itemLogger));
               },
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

        /// <summary>
        /// A log subscription statistics payload
        /// </summary>
        [JobPayload(JobPayloadNameOption.Name)]
        public class Payload : JobPayload
        {
        }
    }
}
