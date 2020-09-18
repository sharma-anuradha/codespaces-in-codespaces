// <copyright file="ResourceHeartbeatContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    public class ResourceHeartbeatContinuationHandler : IResourceHeartbeatContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "JobProcessResourceHeartbeat";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceHeartBeatManager"/> class.
        /// </summary>
        /// <param name="resourceRepository">Resource Repository.</param>
        /// <param name="resourceStateManager">Resource state Manager.</param>
        public ResourceHeartbeatContinuationHandler(
            IResourceRepository resourceRepository,
            IResourceStateManager resourceStateManager,
            IMapper mapper)
        {
            ResourceRepository = resourceRepository;
            ResourceStateManager = resourceStateManager;
            Mapper = mapper;
        }

        private IResourceRepository ResourceRepository { get; }

        private IResourceStateManager ResourceStateManager { get; }

        private IMapper Mapper { get; }

        public bool CanHandle(ContinuationQueuePayload payload)
        {
            return payload.Target == DefaultQueueTarget;
        }

        public Task<ContinuationResult> Continue(ContinuationInput input, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                "heartbeat_continuation",
                async (childLogger) =>
                {
                    var heartbeatInput = (ResourceHeartbeatContinuationInput)input;
                    ContinuationResult result = default;

                    childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, heartbeatInput.ResourceId)
                    .FluentAddBaseValue("HeartbeatTimeStamp", heartbeatInput.HeartBeatData.TimeStamp);

                    if (heartbeatInput.ContinuationToken == default)
                    {
                        input.ContinuationToken = heartbeatInput.ResourceId.ToString();
                        result = new ContinuationResult()
                        {
                            NextInput = input,
                            Status = OperationState.InProgress,
                        };
                    }
                    else
                    {
                        var resourceRecord = await ResourceRepository.GetAsync(heartbeatInput.ResourceId.ToString(), childLogger.NewChildLogger());

                        // Resource is not found in resource db, so no need to process heartbeat.
                        if (resourceRecord == null)
                        {
                            var message = $"No resources found with id {heartbeatInput.ResourceId} specified in the heartbeat";
                            throw new InvalidOperationException(message);
                        }

                        // Resource is ready and last heartbeat was received less than a minute ago, then cancel heartbeat processing.
                        if (resourceRecord.IsReady && resourceRecord.HeartBeatSummary?.LastSeen >= (heartbeatInput.HeartBeatData.TimeStamp - TimeSpan.FromSeconds(60)))
                        {
                            throw new OperationCanceledException("ThrottleHeartbeat");
                        }

                        var resourceHeartBeatRecord = Mapper.Map<ResourceHeartBeatRecord>(heartbeatInput.HeartBeatData);
                        var mergedCollectedDataList = MergeCollectedData(resourceRecord.HeartBeatSummary?.MergedHeartBeat?.CollectedDataList, resourceHeartBeatRecord.CollectedDataList);
                        resourceHeartBeatRecord.CollectedDataList = mergedCollectedDataList;

                        var heartBeatSummaryRecord = new ResourceHeartBeatSummaryRecord
                        {
                            MergedHeartBeat = resourceHeartBeatRecord,
                            LatestRawHeartBeat = Mapper.Map<ResourceHeartBeatRecord>(heartbeatInput.HeartBeatData),
                            LastSeen = heartbeatInput.HeartBeatData.TimeStamp,
                        };

                        resourceRecord.HeartBeatSummary = heartBeatSummaryRecord;

                        if (!resourceRecord.IsReady)
                        {
                            // Update resource status.
                            await ResourceStateManager.MarkResourceReady(resourceRecord, "HeartbeatReceived", childLogger.NewChildLogger());
                        }
                        else
                        {
                            await ResourceRepository.UpdateAsync(resourceRecord, logger.NewChildLogger());
                        }

                        result = new ContinuationResult() { Status = OperationState.Succeeded };
                    }

                    childLogger.FluentAddValue("ContinuationToken", result.NextInput?.ContinuationToken)
                        .FluentAddValue("Status", result.Status)
                        .FluentAddValue("ErrorReason", result.ErrorReason)
                        .FluentAddValue("RetryAfter", result.RetryAfter);

                    return result;
                },
                (ex, childLoggger) =>
                {
                    ContinuationResult errorResult = default;

                    // Heartbeat processing was aborted.
                    if (ex is OperationCanceledException)
                    {
                        errorResult = new ContinuationResult() { Status = OperationState.Cancelled, ErrorReason = ex.Message };
                    }

                    // Heartbeat processing failed with error.
                    errorResult = new ContinuationResult() { Status = OperationState.Failed, ErrorReason = ex.Message };

                    childLoggger.FluentAddValue("ContinuationToken", errorResult.NextInput?.ContinuationToken)
                        .FluentAddValue("Status", errorResult.Status)
                        .FluentAddValue("ErrorReason", errorResult.ErrorReason)
                        .FluentAddValue("RetryAfter", errorResult.RetryAfter);

                    return Task.FromResult(errorResult);
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
    }
}