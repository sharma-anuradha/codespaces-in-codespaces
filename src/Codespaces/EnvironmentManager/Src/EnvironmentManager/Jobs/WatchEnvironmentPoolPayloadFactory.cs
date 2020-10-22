// <copyright file="WatchEnvironmentPoolPayloadFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Jobs
{
    /// <summary>
    /// A class that implements IJobSchedulePayloadFactory and able to produce job payloads
    /// for the watch pool consumer handlers.
    /// </summary>
    public class WatchEnvironmentPoolPayloadFactory : IJobSchedulePayloadFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchEnvironmentPoolPayloadFactory"/> class.
        /// </summary>
        /// <param name="poolDefinitionStore">Resource scalling store.</param>
        public WatchEnvironmentPoolPayloadFactory(
            IEnvironmentPoolDefinitionStore poolDefinitionStore)
        {
            PoolDefinitionStore = Requires.NotNull(poolDefinitionStore, nameof(poolDefinitionStore));
        }

        private IEnvironmentPoolDefinitionStore PoolDefinitionStore { get; }

        /// <inheritdoc/>
        public async Task CreatePayloadsAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, OnPayloadsCreatedDelegateAsync onPayloadCreated, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            await logger.OperationScopeAsync(
               $"{EnvironmentLoggingConstants.WatchEnvironmentPoolSizeTask}_produce_payload",
               async (innerLogger) =>
               {
                   // Get current catalog
                   var pools = await RetrievePoolDefinitions();
                   int poolCount = pools.Count();
                   var jobInvisibleDeltaInMillisecs = 60 * 1000 / (4 * poolCount);
                   var payloadVisibilityDelayHelper = new PayloadVisibilityDelayHelper(TimeSpan.FromMilliseconds(jobInvisibleDeltaInMillisecs));

                   logger.FluentAddValue("EnvironmentPoolCount", poolCount.ToString());

                   await CreateResourcePoolJobsAsync(pools, () => payloadVisibilityDelayHelper.NextValue(), onPayloadCreated, innerLogger);
               });
        }

        private async Task CreateResourcePoolJobsAsync(IEnumerable<EnvironmentPool> pools, Func<TimeSpan> payloadVisibilitCallback, OnPayloadsCreatedDelegateAsync onPayloadCreated, IDiagnosticsLogger logger)
        {
            await onPayloadCreated.AddAllPayloadsAsync(
                pools,
                (pool) =>
                {
                    var loggerProperties = new Dictionary<string, string>()
                    {
                        { "TaskRunId", Guid.NewGuid().ToString() },
                        { EnvironmentLoggingPropertyConstants.PoolLocation, pool.Details.Location.ToString() },
                        { EnvironmentLoggingPropertyConstants.PoolSkuName, pool.Details.SkuName },
                        { EnvironmentLoggingPropertyConstants.PoolDefinition, pool.Details.GetPoolDefinition() },
                        { EnvironmentLoggingPropertyConstants.PoolTargetCount, pool.TargetCount.ToString() },
                        { EnvironmentLoggingPropertyConstants.PoolIsEnabled, pool.IsEnabled.ToString() },
                        { EnvironmentLoggingPropertyConstants.MaxCreateBatchCount, pool.MaxCreateBatchCount.ToString() },
                        { EnvironmentLoggingPropertyConstants.MaxDeleteBatchCount, pool.MaxDeleteBatchCount.ToString() },
                    };

                    logger.FluentAddBaseValues(loggerProperties);

                    // Add payloads for all pool jobs.
                    return CreatePoolJobPayload<WatchEnvironmentPoolSizeJobHandler>(pool, payloadVisibilitCallback, loggerProperties, logger);
                });
        }

        private (EnvironmentPoolPayload<TJobHandlerType>, JobPayloadOptions) CreatePoolJobPayload<TJobHandlerType>(
            EnvironmentPool pool,
            Func<TimeSpan> payloadVisibilitCallback,
            IDictionary<string, string> loggerProperties,
            IDiagnosticsLogger logger)
             where TJobHandlerType : class
        {
            var jobPayload = new EnvironmentPoolPayload<TJobHandlerType>() { PoolId = pool.Id, LoggerProperties = loggerProperties.CreateLoggerProperties() };
            var jobPayloadOptions = new JobPayloadOptions()
            {
                InitialVisibilityDelay = payloadVisibilitCallback(),
                ExpireTimeout = JobPayloadOptions.DefaultJobPayloadExpireTimeout,
            };

            return (jobPayload, jobPayloadOptions);
        }

        private async Task<IList<EnvironmentPool>> RetrievePoolDefinitions()
        {
            return (await PoolDefinitionStore.RetrieveDefinitionsAsync()).Shuffle().ToList();
        }

        /// <summary>
        /// A resource pool payload.
        /// </summary>
        /// <typeparam name="T">Type of the job handler.</typeparam>
        public class EnvironmentPoolPayload<T> : JobPayload<T>
            where T : class
        {
            /// <summary>
            /// Gets or sets the pool id.
            /// </summary>
            public string PoolId { get; set; }
        }

        private class PayloadVisibilityDelayHelper
        {
            private readonly TimeSpan space;
            private TimeSpan next = TimeSpan.Zero;

            public PayloadVisibilityDelayHelper(TimeSpan space)
            {
                this.space = space;
            }

            public TimeSpan NextValue()
            {
                TimeSpan result = next;
                next = next.Add(space);
                return result;
            }
        }
    }
}
