// <copyright file="WarmupController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.States;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApi.Support;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackendWebApi.Controllers
{
    /// <summary>
    /// This controller is used to warmup dependent service connections.
    /// We use this to prevent traffic from hitting the service until everything is ready.
    /// </summary>
    [ApiController]
    [Route("warmup")]
    public class WarmupController : ControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WarmupController"/> class.
        /// </summary>
        /// <param name="triggerWarmup"></param>
        public WarmupController(ITriggerWarmup triggerWarmup)
        {
            TriggerWarmup = triggerWarmup;
        }

        private ITriggerWarmup TriggerWarmup { get; }

        /// <summary>
        ///
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetAsync()
        {
            var resultHttpStatus = TriggerWarmup.Start();

            return StatusCode(resultHttpStatus);
        }

    }
}
