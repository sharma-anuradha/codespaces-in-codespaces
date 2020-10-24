// <copyright file="CloudEnvironmentRegionalMigrationJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
    public class CloudEnvironmentRegionalMigrationJobHandler : JobHandlerPayloadBase<GuidShardJobProducer.GuidShardPayload<CloudEnvironmentRegionalMigrationJobHandler>>, IGuidShardJobScheduleDetails
    {
        private static readonly TimeSpan QueryDelay = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentRegionalMigrationJobHandler"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        public CloudEnvironmentRegionalMigrationJobHandler(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IControlPlaneInfo controlPlaneInfo)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
        }

        public string EnabledFeatureFlagName => "EnvironmentManagerJob";

        public string JobName => "cloud_environment_regional_migration_task";

        public string QueueId => EnvironmentJobQueueConstants.GenericQueueName;

        public (string CronExpression, TimeSpan Interval) ScheduleTimeInterval => JobPayloadRegisterSchedule.CloudEnvironmentRegionalMigrationJobSchedule;

        public Type PayloadTagType => typeof(CloudEnvironmentRegionalMigrationJobHandler);

        private string LogBaseName => EnvironmentLoggingConstants.CloudEnvironmentRegionalMigrationTask;

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        protected override async Task HandleJobAsync(GuidShardJobProducer.GuidShardPayload<CloudEnvironmentRegionalMigrationJobHandler> payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            var idShard = payload.Shard;

            logger.FluentAddValue("TaskEnvironmentIdShard", idShard);

            await logger.OperationScopeAsync(
                $"{LogBaseName}_process_shard",
                async (loopLogger) =>
                {
                    await CloudEnvironmentRepository.GlobalRepository.ForEachAsync(
                        (x) => x.Id.StartsWith(idShard) && x.Location == ControlPlaneInfo.Stamp.Location,
                        loopLogger.NewChildLogger(),
                        (environment, childLogger) =>
                        {
                            if (environment.IsMigrated)
                            {
                                return Task.CompletedTask;
                            }

                            return CoreRunUnitAsync(environment, childLogger);
                        },
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

                    environment.IsMigrated = true;

                    try
                    {
                        await CloudEnvironmentRepository.RegionalRepository.CreateOrUpdateAsync(environment, logger.NewChildLogger());
                    }
                    catch (DocumentClientException ex)
                    {
                        // Note: If we get a Precondition Failed error, it means that the environment has already been copied to the Regional DB.
                        if (ex.StatusCode != System.Net.HttpStatusCode.PreconditionFailed)
                        {
                            throw;
                        }
                    }

                    await CloudEnvironmentRepository.GlobalRepository.UpdateAsync(environment, logger.NewChildLogger());
                },
                swallowException: true);
        }
    }
}
