// <copyright file="HealthMonitorController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApi.Controllers
{
    /// <summary>
    /// Health monitor controller for the backend.
    /// </summary>
    [ApiController]
    [Route("healthmonitor")]
    [LoggingBaseName("backend_health_monitor_controller")]
    public class HealthMonitorController : ControllerBase
    {
    }
}
