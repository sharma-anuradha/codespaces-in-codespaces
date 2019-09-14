// <copyright file="HealthMonitorController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// Health monitor controller for the front end.
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthenticationBuilderVMTokenExtensions.AuthenticationScheme)]
    [Route("healthmonitor")]
    [FriendlyExceptionFilter]
    [LoggingBaseName("frontend_health_monitor_controller")]
    public class HealthMonitorController : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Post()
        {
            // call IsValidResource.
            return NoContent();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Pending code.")]
        private bool IsValidResource(string resourceId)
        {
            var vmResourceId = HttpContext.Items[AuthenticationBuilderVMTokenExtensions.VMResourceIdName] as string;
            if (string.IsNullOrWhiteSpace(vmResourceId))
            {
                return false;
            }

            if (vmResourceId != resourceId)
            {
                return false;
            }

            return true;
        }
    }
}
