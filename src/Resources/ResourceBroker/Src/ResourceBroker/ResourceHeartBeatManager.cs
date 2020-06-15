// <copyright file="ResourceHeartBeatManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
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
        public ResourceHeartBeatManager(IResourceRepository resourceRepository, IMapper mapper)
        {
            ResourceRepository = resourceRepository;
            Mapper = mapper;
        }

        private IResourceRepository ResourceRepository { get; }

        private IMapper Mapper { get; }

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

                  var resourceRecord = await ResourceRepository.GetAsync(heartBeatInput.ResourceId.ToString(), logger.NewChildLogger());
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

                  if (!resourceRecord.IsReady)
                  {
                      // Storage resources are ready once they are provisioned. While compute resources are ready when heartbeat is received.
                      resourceRecord.IsReady = true;
                      resourceRecord.Ready = DateTime.UtcNow;
                  }

                  // Update os disk record if it exists.
                  var computeDetails = resourceRecord.GetComputeDetails();
                  var osDiskRecordId = computeDetails.OSDiskRecordId;
                  if (osDiskRecordId != default)
                  {
                      var osDiskResourceRecord = await ResourceRepository.GetAsync(osDiskRecordId.ToString(), logger.NewChildLogger());
                      if (osDiskResourceRecord != default)
                      {
                          if (!osDiskResourceRecord.IsReady)
                          {
                              osDiskResourceRecord.IsReady = true;
                              osDiskResourceRecord.Ready = DateTime.UtcNow;
                          }

                          // Copies over heartbeat information to OSDisk as well. When compute is gone, we will rely on the information in the OSDisk.
                          osDiskResourceRecord.HeartBeatSummary = heartBeatSummaryRecord;

                          await ResourceRepository.UpdateAsync(osDiskResourceRecord, logger.NewChildLogger());
                      }
                  }

                  await ResourceRepository.UpdateAsync(resourceRecord, logger.NewChildLogger());
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
