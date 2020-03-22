// <copyright file="HeartBeatController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring;
using Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring.DataHandlers;
using Newtonsoft.Json;
using static Microsoft.VsSaaS.Diagnostics.Extensions.DiagnosticsLoggerExtensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// Health monitor controller for the front end.
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthenticationBuilderVMTokenExtensions.AuthenticationScheme)]
    [Route(ServiceConstants.ApiV1Route)]
    [FriendlyExceptionFilter]
    [LoggingBaseName("frontend_heartbeat_controller")]
    public class HeartBeatController : ControllerBase
    {
        private readonly IEnvironmentManager environmentManager;
        private IEnumerable<IDataHandler> handlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatController"/> class.
        /// </summary>
        /// <param name="handlers">List of handlers to process the collected data from VSOAgent.</param>
        /// <param name="backendHeartBeatClient">Backend HeartBeat Client.</param>
        /// <param name="environmentManager">Environment Manager.</param>
        public HeartBeatController(
            IEnumerable<IDataHandler> handlers,
            IResourceHeartBeatHttpContract backendHeartBeatClient,
            IEnvironmentManager environmentManager)
        {
            this.handlers = handlers;
            BackendHeartBeatClient = backendHeartBeatClient;
            this.environmentManager = environmentManager;
        }

        private IResourceHeartBeatHttpContract BackendHeartBeatClient { get; }

        /// <summary>
        /// Controller to recieve heartbeat messages from VSO Agents.
        /// </summary>
        /// <param name="heartBeat">HeartBeat message.</param>
        /// <returns>No data.</returns>
        [HttpPost]
        [ThrottlePerUserHigh(nameof(HeartBeatController), nameof(ProcessHeartBeatAsync))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ProcessHeartBeatAsync([FromBody] HeartBeatBody heartBeat)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            logger.AddComputeResourceId(heartBeat.ResourceId)
                  .FluentAddBaseValue(nameof(heartBeat.AgentVersion), heartBeat.AgentVersion);

            try
            {
                ValidateHeartbeat(heartBeat, logger);
            }
            catch (Exception e)
            {
                LogHeartBeatException(e, e.Message, logger, duration, heartBeat);
                return UnprocessableEntity();
            }

            var shouldSendBackendTask = true;
            if (!string.IsNullOrWhiteSpace(heartBeat.EnvironmentId))
            {
                var environment = await environmentManager.GetAsync(heartBeat.EnvironmentId, logger);
                if (environment.Type != EnvironmentType.StaticEnvironment)
                {
                    shouldSendBackendTask = false;
                }
            }

            var backendTask = shouldSendBackendTask ?
                BackendHeartBeatClient.UpdateHeartBeatAsync(heartBeat.ResourceId, heartBeat, logger.NewChildLogger()) :
                Task.CompletedTask;

            var collectedData = heartBeat?.CollectedDataList?.Where(data => data != null);
            if (collectedData != null && collectedData.Count() > 0)
            {
                var processingExceptions = new List<Exception>();

                await logger.OperationScopeAsync(
                   "process_heartbeat_collected_data",
                   async (childLogger) =>
                   {
                       CloudEnvironment environment = null;

                       var environmentId = collectedData.FirstOrDefault(c => c.EnvironmentId != default)?.EnvironmentId;
                       if (!string.IsNullOrWhiteSpace(environmentId))
                       {
                           environment = await environmentManager.GetAsync(environmentId, childLogger);
                       }

                       var handlerContext = new CollectedDataHandlerContext(environment);

                       foreach (var data in collectedData)
                       {
                           var handler = handlers.Where(h => h.CanProcess(data)).FirstOrDefault();

                           if (handler != null)
                           {
                               try
                               {
                                   handlerContext = await handler.ProcessAsync(data, handlerContext, heartBeat.ResourceId, logger.NewChildLogger());
                               }
                               catch (Exception e)
                               {
                                   LogHeartBeatException(e, $"Processing failed for the data {data.Name} received from Virtual Machine {heartBeat.ResourceId}", logger, duration, heartBeat);

                                   // Collect exceptions inorder to give a chance to every CollectedData object to go through processing before throwing
                                   processingExceptions.Add(e);
                               }
                           }
                           else
                           {
                               logger.AddDuration(duration)
                                   .FluentAddValue("HeartbeatMessage", JsonConvert.SerializeObject(heartBeat))
                                   .LogWarning($"No handlers found for processing the data {data?.Name} received from Virtual Machine {heartBeat.ResourceId}");
                           }
                       }

                       await UpdateCloudEnvironment(handlerContext, logger);
                   });

                if (processingExceptions.Any())
                {
                    // If all the exceptions are ValidationException, then return 422, to prevent agent from retrying.
                    if (processingExceptions.All(e => e is ValidationException))
                    {
                        return UnprocessableEntity();
                    }

                    // If any of the handlers returned an exception other than ValidationException, then return 500 to make the agent retry.
                    LogHeartBeatException(new AggregateException(processingExceptions), $"One or more handlers failed due to unexpected exceptions.", logger, duration, heartBeat);
                    return InternalServerError();
                }
            }

            try
            {
                await backendTask;
            }
            catch (HttpResponseStatusException e)
            {
                LogHeartBeatException(e, $"Backend heartbeat processing failed with http status code {e.StatusCode}", logger, duration, heartBeat);
                return StatusCode((int)e.StatusCode);
            }
            catch (RemoteInvocationException e)
            {
                // Currently exceptions from backend heartbeat controller will always result
                // in HttpResponseStatusException, as it doesn't respond with errordetails in body.
                LogHeartBeatException(e, "Backend heartbeat processing failed with RemoteInvocationException.", logger, duration, heartBeat);
                return UnprocessableEntity();
            }
            catch (Exception e)
            {
                LogHeartBeatException(e, "Backend heartbeat processing failed.", logger, duration, heartBeat);
                return InternalServerError();
            }

            logger.AddDuration(duration)
                .LogInfo(GetType().FormatLogMessage(nameof(ProcessHeartBeatAsync)));

            return NoContent();
        }

        private async Task UpdateCloudEnvironment(CollectedDataHandlerContext handlerContext, IDiagnosticsLogger logger)
        {
            if (handlerContext.CloudEnvironment == null)
            {
                return;
            }

            await environmentManager.UpdateAsync(handlerContext.CloudEnvironment, handlerContext.CloudEnvironmentState, handlerContext.Trigger ?? CloudEnvironmentStateUpdateTriggers.Heartbeat, handlerContext.Reason ?? string.Empty, logger);
        }

        private IActionResult InternalServerError() => StatusCode(StatusCodes.Status500InternalServerError);

        private void LogHeartBeatException(Exception e, string message, IDiagnosticsLogger logger, Duration startDuration, HeartBeatBody heartBeat)
        {
            logger.AddDuration(startDuration)
                .FluentAddValue("HeartbeatMessage", JsonConvert.SerializeObject(heartBeat))
                .FluentAddValue("HeartbeatDescription", message)
                .LogException("frontend_heartbeat_processing_error", e);
        }

        private void ValidateHeartbeat(HeartBeatBody heartbeat, IDiagnosticsLogger logger)
        {
            ValidationUtil.IsTrue(HttpContext.Items.ContainsKey(AuthenticationBuilderVMTokenExtensions.VMResourceIdName), "Heartbeat token has no resourceId");
            ValidationUtil.IsTrue(Guid.TryParse(HttpContext.Items[AuthenticationBuilderVMTokenExtensions.VMResourceIdName] as string, out var tokenResourceId), $"Heartbeat token has invalid resourceId");
            ValidationUtil.IsTrue(heartbeat.ResourceId != default, $"Heartbeat received with empty resourceId in body, and token resourceId {tokenResourceId}");
            ValidationUtil.IsTrue(tokenResourceId == heartbeat.ResourceId, $"Heartbeat received with conflicting resourceId in body ({heartbeat.ResourceId}), and in token ({tokenResourceId})");
            if (!string.IsNullOrWhiteSpace(heartbeat.EnvironmentId))
            {
                var collectedDataEnvIds = heartbeat.CollectedDataList.Select(d => d.EnvironmentId).Where(id => id != default);
                ValidationUtil.IsTrue(collectedDataEnvIds.All(id => id == heartbeat.EnvironmentId), $"Heartbeat received with conflicting environmentId in body and in collected data");
            }
        }
    }
}
