// <copyright file="LogController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiagnosticsServer.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DiagnosticsServer.Controllers
{
    /// <summary>
    /// The Log Controller.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LogController : ControllerBase
    {
        private readonly IHubContext<LogHub> logHub;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogController"/> class.
        /// </summary>
        /// <param name="logHub">The Log Hub.</param>
        public LogController(IHubContext<LogHub> logHub)
        {
            this.logHub = logHub;
        }
    }
}
