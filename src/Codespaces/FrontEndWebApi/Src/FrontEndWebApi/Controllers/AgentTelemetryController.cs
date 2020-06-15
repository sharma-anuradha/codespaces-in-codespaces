// <copyright file="AgentTelemetryController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.VsoAgent;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// Vso Agent Telemetry controller for the front end.
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthenticationBuilderVMTokenExtensions.AuthenticationScheme)]
    [Route(ServiceConstants.ApiV1Route)]
    [FriendlyExceptionFilter]
    [LoggingBaseName("frontend_agenttelemetry_controller")]
    public class AgentTelemetryController : ControllerBase
    {
        private const string LogValueComputeResourceId = "ComputeResourceId";

        /// <summary>
        /// Controller to recieve telemetry collection from VSO Agents.
        /// </summary>
        /// <param name="telemetryDataCollection">Collection of <see cref="TelemetryData"/>.</param>
        /// <returns>No data.</returns>
        [HttpPost]
        [ThrottlePerUserHigh(nameof(AgentTelemetryController), nameof(ProcessTelemetryAsync))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult ProcessTelemetryAsync([FromBody] IEnumerable<TelemetryData> telemetryDataCollection)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            Guid vmResourceId;
            try
            {
                vmResourceId = ValidateVmTokenAndGetResourceId();
                logger.AddComputeResourceId(vmResourceId);
            }
            catch (ValidationException e)
            {
                logger.AddDuration(duration)
                    .LogException(e.Message, e);
                return Unauthorized();
            }

            var telemetryLogger = new JsonStdoutLogger(new LogValueSet());
            telemetryLogger.AddBaseValue("Service", "VsoAgent");

            var validationExceptions = new List<ValidationException>();
            foreach (var data in telemetryDataCollection)
            {
                try
                {
                    ValidateTelemetryComputeResourceId(data, vmResourceId);

                    var childLogger = telemetryLogger.NewChildLogger();

                    // Use the time from the agent telemetry
                    childLogger.AddBaseValue("time", data.Time);

                    foreach (var optionalKvp in data.OptionalValues)
                    {
                        childLogger.AddBaseValue(optionalKvp.Key, optionalKvp.Value.ToString());
                    }

                    childLogger.Log(data.Message, data.Level);
                }
                catch (ValidationException e)
                {
                    validationExceptions.Add(e);
                }
            }

            if (validationExceptions.Any())
            {
                logger.AddDuration(duration)
                    .LogException("vsoagent_telemetry_validation_failed", new AggregateException(validationExceptions));
            }

            return NoContent();
        }

        private bool ValidateTelemetryComputeResourceId(TelemetryData data, Guid vmResourceId)
        {
            var logComputeId = data.OptionalValues.GetValueOrDefault(LogValueComputeResourceId);

            ValidationUtil.IsTrue(Guid.TryParse(logComputeId, out var logComputeIdGuid), $"VsoAgent telemetry entry received with empty {LogValueComputeResourceId}, from the VM {vmResourceId}");
            ValidationUtil.IsTrue(logComputeIdGuid != default, $"VsoAgent telemetry entry received with empty {LogValueComputeResourceId}, from the VM {vmResourceId}");
            ValidationUtil.IsTrue(vmResourceId == logComputeIdGuid, $"VsoAgent telemetry entry received with conflicting {LogValueComputeResourceId} = {logComputeId}, from the VM {vmResourceId}");

            return false;
        }

        private Guid ValidateVmTokenAndGetResourceId()
        {
            ValidationUtil.IsTrue(HttpContext.Items.ContainsKey(AuthenticationBuilderVMTokenExtensions.VMResourceIdName), "VsoAgent telemetry VMToken has invalid vmResourceId");
            ValidationUtil.IsTrue(Guid.TryParse(HttpContext.Items[AuthenticationBuilderVMTokenExtensions.VMResourceIdName] as string, out var vmResourceId), "VsoAgent telemetry VMToken has invalid vmResourceId");
            ValidationUtil.IsTrue(vmResourceId != default, "VsoAgent telemetry VMToken has invalid vmResourceId");

            return vmResourceId;
        }
    }
}
