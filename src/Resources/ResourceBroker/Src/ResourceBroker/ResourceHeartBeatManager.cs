// <copyright file="ResourceHeartBeatManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Azure.Documents;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Manages heartbeat messages.
    /// </summary>
    public class ResourceHeartBeatManager : IResourceHeartBeatManager
    {
        private const string LogBaseName = ResourceLoggingConstants.ResourceHeartBeatManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceHeartBeatManager"/> class.
        /// </summary>
        /// <param name="resourceRepository">Resource Repository.</param>
        /// <param name="mapper">Mapper.</param>
        /// <param name="resourceStateManager">Resource state Manager.</param>
        public ResourceHeartBeatManager(
            IResourceRepository resourceRepository,
            IMapper mapper,
            IResourceStateManager resourceStateManager)
        {
            ResourceRepository = resourceRepository;
            Mapper = mapper;
            ResourceStateManager = resourceStateManager;
        }

        private IResourceRepository ResourceRepository { get; }

        private IMapper Mapper { get; }

        private IResourceStateManager ResourceStateManager { get; }

        /// <inheritdoc/>
        public async Task SaveHeartBeatAsync(HeartBeatInput heartBeatInput, IDiagnosticsLogger logger)
        {
            await logger.RetryOperationScopeAsync(
              $"{LogBaseName}_save_heartbeat",
              async (childLogger) =>
              {
                  if (heartBeatInput == null)
                  {
                      childLogger.LogError($"Required argument {nameof(heartBeatInput)} is null.");
                      throw new ArgumentNullException(nameof(heartBeatInput));
                  }

                  var resourceRecord = await ResourceRepository.GetAsync(heartBeatInput.ResourceId.ToString(), childLogger.NewChildLogger());
                  if (resourceRecord == null)
                  {
                      var message = $"No resources found with id {heartBeatInput.ResourceId} specified in the heartbeat";
                      childLogger.LogError(message);
                      throw new ResourceNotFoundException(heartBeatInput.ResourceId);
                  }

                  var resourceHeartBeatRecord = Mapper.Map<ResourceHeartBeatRecord>(heartBeatInput);
                  var mergedCollectedDataList = MergeCollectedData(resourceRecord.HeartBeatSummary?.MergedHeartBeat?.CollectedDataList, resourceHeartBeatRecord.CollectedDataList);
                  resourceHeartBeatRecord.CollectedDataList = mergedCollectedDataList;

                  var heartBeatSummaryRecord = new ResourceHeartBeatSummaryRecord
                  {
                      MergedHeartBeat = resourceHeartBeatRecord,
                      LatestRawHeartBeat = Mapper.Map<ResourceHeartBeatRecord>(heartBeatInput),
                      LastSeen = heartBeatInput.TimeStamp,
                  };

                  resourceRecord.HeartBeatSummary = heartBeatSummaryRecord;

                  // Update os disk record if it exists.
                  var computeDetails = resourceRecord.GetComputeDetails();
                  var osDiskRecordId = computeDetails.OSDiskRecordId;
                  if (osDiskRecordId != default)
                  {
                      var osDiskResourceRecord = await ResourceRepository.GetAsync(osDiskRecordId.ToString(), childLogger.NewChildLogger());
                      if (osDiskResourceRecord != default)
                      {
                          if (!osDiskResourceRecord.IsReady)
                          {
                              osDiskResourceRecord.IsReady = true;
                              osDiskResourceRecord.Ready = DateTime.UtcNow;
                          }

                          // Copies over heartbeat information to OSDisk as well. When compute is gone, we will rely on the information in the OSDisk.
                          osDiskResourceRecord.HeartBeatSummary = heartBeatSummaryRecord;

                          await ResourceRepository.UpdateAsync(osDiskResourceRecord, childLogger.NewChildLogger());
                      }
                  }

                  if (!resourceRecord.IsReady)
                  {
                      // Update resource status.
                      await ResourceStateManager.MarkResourceReady(resourceRecord, "HeartbeatReceived", childLogger.NewChildLogger());
                  }
                  else
                  {
                      var updateLogger = childLogger.NewChildLogger();

                      await Retry.DoAsync(
                         async (int attemptNumber) =>
                         {
                             updateLogger.AddAttempt(attemptNumber);

                             await ResourceRepository.UpdateAsync(resourceRecord, updateLogger.NewChildLogger());
                         },
                         async (attemptNumber, ex) =>
                         {
                             updateLogger.AddAttempt(attemptNumber);

                             if (ex is DocumentClientException dcex && dcex.StatusCode == HttpStatusCode.PreconditionFailed)
                             {
                                 resourceRecord = await ResourceRepository.GetAsync(osDiskRecordId.ToString(), updateLogger.NewChildLogger());
                                 resourceRecord.HeartBeatSummary = heartBeatSummaryRecord;
                             }
                         });
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
    }
}
