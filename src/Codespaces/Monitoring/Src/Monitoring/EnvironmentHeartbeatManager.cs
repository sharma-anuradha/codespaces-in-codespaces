// <copyright file="EnvironmentHeartbeatManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring.DataHandlers;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring
{
    public class EnvironmentHeartbeatManager : IEnvironmentHeartbeatManager
    {
        private const string LogBaseName = "environment_heartbeat_manager";

        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatController"/> class.
        /// </summary>
        /// <param name="handlers">List of handlers to process the collected data from VSOAgent.</param>
        /// <param name="heartbeatRepository">Heartbeat repository.</param>
        /// <param name="cloudEnvironmentRepository">cloud environment repository.</param>
        /// <param name="latestHeartbeatMonitor">Latest Heartbeat Monitor.</param>
        /// <param name="environmentStateManager">Environment state manager.</param>
        public EnvironmentHeartbeatManager(
            IEnumerable<IDataHandler> handlers,
            ICloudEnvironmentHeartbeatRepository heartbeatRepository,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            ILatestHeartbeatMonitor latestHeartbeatMonitor,
            IEnvironmentStateManager environmentStateManager)
        {
            Handlers = handlers;
            HeartbeatRepository = heartbeatRepository;
            CloudEnvironmentRepository = cloudEnvironmentRepository;
            LatestHeartbeatMonitor = latestHeartbeatMonitor;
            EnvironmentStateManager = environmentStateManager;
        }

        private IEnumerable<IDataHandler> Handlers { get; }

        private ICloudEnvironmentHeartbeatRepository HeartbeatRepository { get; }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private ILatestHeartbeatMonitor LatestHeartbeatMonitor { get; }

        private IEnvironmentStateManager EnvironmentStateManager { get; }

        /// <inheritdoc/>
        public async Task<IEnumerable<Exception>> ProcessCollectedDataAsync(HeartBeatBody heartBeat, CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            environment = await UpdateEnvironmentHeartbeatAsync(heartBeat, environment, logger);

            var collectedData = heartBeat?.CollectedDataList?.Where(data => data != null);

            if (collectedData == null || collectedData.Count() == 0)
            {
                return Enumerable.Empty<Exception>();
            }

            return await logger.OperationScopeAsync(
               "process_heartbeat_collected_data",
               async (childLogger) =>
               {
                   var handlerContext = new CollectedDataHandlerContext(BuildTransition(environment));
                   var processingExceptions = new List<Exception>();

                   foreach (var data in collectedData)
                   {
                       var handler = Handlers.Where(h => h.CanProcess(data)).FirstOrDefault();

                       if (handler != null)
                       {
                           try
                           {
                               handlerContext = await handler.ProcessAsync(data, handlerContext, heartBeat.ResourceId, logger.NewChildLogger());

                               if (handlerContext.StopProcessing)
                               {
                                   // stop processing as terminating update like suspend environment has been done.
                                   break;
                               }
                           }
                           catch (Exception e)
                           {
                               // Collect exceptions inorder to give a chance to every CollectedData object to go through processing before throwing
                               processingExceptions.Add(e);
                           }
                       }
                       else
                       {
                           logger.FluentAddValue("HeartbeatMessage", JsonConvert.SerializeObject(heartBeat))
                              .FluentAddValue("HeartbeatDataName", data?.Name)
                              .FluentAddValue("HeartbeatResourceId", heartBeat.ResourceId)
                              .LogWarning($"{LogBaseName}_no_handler_found_error");
                       }
                   }

                   await UpdateCloudEnvironment(handlerContext, logger);

                   return processingExceptions;
               });
        }

        private async Task<CloudEnvironment> UpdateEnvironmentHeartbeatAsync(HeartBeatBody heartBeat, CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            var heartbeatResourceId = environment.HeartbeatResourceId;

            if (string.IsNullOrEmpty(heartbeatResourceId))
            {
                var heartbeatRecord = new CloudEnvironmentHeartbeat() { LastUpdatedByHeartBeat = heartBeat.TimeStamp };
                heartbeatRecord = await HeartbeatRepository.CreateAsync(heartbeatRecord, logger.NewChildLogger());

                await logger.RetryOperationScopeAsync(
                    $"process_heartbeat_migrate_heartbeat",
                    async (innerLogger) =>
                    {
                        environment = await CloudEnvironmentRepository.GetAsync(environment.Id, innerLogger.NewChildLogger());
                        environment.HeartbeatResourceId = heartbeatRecord.Id;
                        environment = await CloudEnvironmentRepository.UpdateAsync(environment, logger.NewChildLogger());
                    });
            }
            else
            {
                await logger.RetryOperationScopeAsync(
                     $"process_heartbeat_heartbeat_update",
                     async (innerLogger) =>
                     {
                         var heartbeatRecord = await HeartbeatRepository.GetAsync(heartbeatResourceId, logger.NewChildLogger());

                         if (heartbeatRecord.LastUpdatedByHeartBeat != default && heartbeatRecord.LastUpdatedByHeartBeat >= heartBeat.TimeStamp)
                         {
                             // no update required as record has latest heartbeat.
                             return;
                         }

                         heartbeatRecord.LastUpdatedByHeartBeat = heartBeat.TimeStamp;
                         await HeartbeatRepository.UpdateAsync(heartbeatRecord, logger.NewChildLogger());
                     });
            }

            LatestHeartbeatMonitor.UpdateHeartbeat(heartBeat.TimeStamp);

            return environment;
        }

        private async Task UpdateCloudEnvironment(CollectedDataHandlerContext handlerContext, IDiagnosticsLogger logger)
        {
            if (handlerContext.CloudEnvironmentTransition?.Value == default)
            {
                return;
            }

            if (handlerContext.CloudEnvironmentState != default && handlerContext.CloudEnvironmentState != handlerContext.CloudEnvironmentTransition.Value.State)
            {
                await EnvironmentStateManager.SetEnvironmentStateAsync(handlerContext.CloudEnvironmentTransition, handlerContext.CloudEnvironmentState, handlerContext.Trigger, handlerContext.Reason, handlerContext.IsUserError, logger.NewChildLogger());
            }

            await CloudEnvironmentRepository.UpdateTransitionAsync("EnvironmentHeartbeatProcess", handlerContext.CloudEnvironmentTransition, logger.NewChildLogger());
        }

        private EnvironmentTransition BuildTransition(CloudEnvironment model)
        {
            return new EnvironmentTransition(model);
        }
    }
}
