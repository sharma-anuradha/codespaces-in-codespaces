// <copyright file="ResourceHeartbeatJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    public class ResourceHeartbeatJobHandler : JobHandlerPayloadBase<ResourceHeartbeatJobHandler.Payload>, IJobHandlerTarget
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueId = "jobhandler-heartbeat";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceHeartBeatManager"/> class.
        /// </summary>
        /// <param name="resourceRepository">Resource Repository.</param>
        /// <param name="resourceStateManager">Resource state Manager.</param>
        public ResourceHeartbeatJobHandler(
            IResourceRepository resourceRepository,
            IResourceStateManager resourceStateManager,
            IMapper mapper)
            : base(options: JobHandlerOptions.WithValues(maxHandlerRetries: 1))
        {
            ResourceRepository = resourceRepository;
            ResourceStateManager = resourceStateManager;
            Mapper = mapper;
        }

        /// <inheritdoc/>
        public IJobHandler JobHandler => this;

        /// <inheritdoc/>
        public string QueueId => DefaultQueueId;

        /// <inheritdoc/>
        public AzureLocation? Location => null;

        private IResourceRepository ResourceRepository { get; }

        private IResourceStateManager ResourceStateManager { get; }

        private IMapper Mapper { get; }

        protected override Task HandleJobAsync(Payload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                "heartbeat_continuation",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, payload.ResourceId)
                        .FluentAddBaseValue("HeartbeatTimeStamp", payload.HeartBeatData.TimeStamp);

                    var resourceRecord = await ResourceRepository.GetAsync(payload.ResourceId.ToString(), childLogger.NewChildLogger());

                    // Resource is not found in resource db, so no need to process heartbeat.
                    if (resourceRecord == null)
                    {
                        var message = $"No resources found with id {payload.ResourceId} specified in the heartbeat";
                        throw new InvalidOperationException(message);
                    }

                    // Resource is ready and last heartbeat was received less than a minute ago, then cancel heartbeat processing.
                    if (resourceRecord.IsReady && resourceRecord.HeartBeatSummary?.LastSeen >= (payload.HeartBeatData.TimeStamp - TimeSpan.FromSeconds(60)))
                    {
                        throw new OperationCanceledException("ThrottleHeartbeat");
                    }

                    var resourceHeartBeatRecord = Mapper.Map<ResourceHeartBeatRecord>(payload.HeartBeatData);
                    var mergedCollectedDataList = MergeCollectedData(resourceRecord.HeartBeatSummary?.MergedHeartBeat?.CollectedDataList, resourceHeartBeatRecord.CollectedDataList);
                    resourceHeartBeatRecord.CollectedDataList = mergedCollectedDataList;

                    var heartBeatSummaryRecord = new ResourceHeartBeatSummaryRecord
                    {
                        MergedHeartBeat = resourceHeartBeatRecord,
                        LatestRawHeartBeat = Mapper.Map<ResourceHeartBeatRecord>(payload.HeartBeatData),
                        LastSeen = payload.HeartBeatData.TimeStamp,
                    };

                    resourceRecord.HeartBeatSummary = heartBeatSummaryRecord;

                    if (!resourceRecord.IsReady)
                    {
                        // Update resource status.
                        await ResourceStateManager.MarkResourceReady(resourceRecord, "HeartbeatReceived", childLogger.NewChildLogger());
                    }
                    else
                    {
                        await ResourceRepository.UpdateAsync(resourceRecord, childLogger.NewChildLogger());
                    }
                });
        }

        /// <summary>
        /// Always preserve the latest copy of the collected data sent by a monitor or a job result, by merging the collected data list rather than replacing.
        /// </summary>
        private IEnumerable<CollectedData> MergeCollectedData(IEnumerable<CollectedData> existingDataList, IEnumerable<CollectedData> newDataList)
        {
            if (existingDataList == null || existingDataList.Count() == 0)
            {
                return newDataList?.Where(newData => newData != null);
            }

            var mergedDataList = existingDataList.Where(data => data != null).ToList();
            if (newDataList != null)
            {
                foreach (var newData in newDataList)
                {
                    if (newData != null)
                    {
                        var existingdata = mergedDataList.Find(existingData => existingData?.Name == newData.Name);
                        if (existingdata != default)
                        {
                            mergedDataList.Remove(existingdata);
                        }

                        mergedDataList.Add(newData);
                    }
                }
            }

            return mergedDataList;
        }

        [JobPayload(JobPayloadNameOption.Name)]
        public class Payload : JobPayload
        {
            /// <summary>
            /// Gets or sets resource id.
            /// </summary>
            public Guid ResourceId { get; set; }

            /// <summary>
            /// Gets or sets heartbeat data.
            /// </summary>
            public HeartBeatInput HeartBeatData { get; set; }
        }
    }
}