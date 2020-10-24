// <copyright file="SyncRegionalEnvironmentsToGlobalJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Producers;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Jobs
{
    public class SyncRegionalEnvironmentsToGlobalJobHandler : JobHandlerPayloadBase<GuidShardJobProducer.GuidShardPayload<SyncRegionalEnvironmentsToGlobalJobHandler>>, IGuidShardJobScheduleDetails
    {
        private static readonly TimeSpan QueryDelay = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncRegionalEnvironmentsToGlobalJobHandler"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        public SyncRegionalEnvironmentsToGlobalJobHandler(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IControlPlaneInfo controlPlaneInfo)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
        }

        public string EnabledFeatureFlagName => "EnvironmentManagerJob";

        public string JobName => "sync_regional_environments_to_global_task";

        public string QueueId => EnvironmentJobQueueConstants.GenericQueueName;

        public (string CronExpression, TimeSpan Interval) ScheduleTimeInterval => JobPayloadRegisterSchedule.SyncRegionalEnvironmentsToGlobalJobSchedule;

        public Type PayloadTagType => typeof(SyncRegionalEnvironmentsToGlobalJobHandler);

        private string LogBaseName => EnvironmentLoggingConstants.SyncRegionalEnvironmentsToGlobalTask;

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        protected override async Task HandleJobAsync(GuidShardJobProducer.GuidShardPayload<SyncRegionalEnvironmentsToGlobalJobHandler> payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            var idShard = payload.Shard;

            logger.FluentAddValue("TaskEnvironmentIdShard", idShard);

            await logger.OperationScopeAsync(
                $"{LogBaseName}_process_shard",
                async (loopLogger) =>
                {
                    await CloudEnvironmentRepository.GlobalRepository.ForEachAsync(
                        (x) => x.Id.StartsWith(idShard) && x.Location == ControlPlaneInfo.Stamp.Location && x.State == CloudEnvironmentState.None,
                        loopLogger.NewChildLogger(),
                        CoreRunUnitAsync,
                        (_, __) => Task.Delay(QueryDelay));
                });
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
