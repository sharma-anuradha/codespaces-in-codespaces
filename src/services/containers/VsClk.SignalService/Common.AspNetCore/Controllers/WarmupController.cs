// <copyright file="WarmupController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService.Controllers
{
    [Route("[controller]")]
    public class WarmupController : ControllerBase
    {
        private readonly WarmupService warmupService;
        private readonly ILogger logger;

        public WarmupController(WarmupService warmupService, ILogger<WarmupController> logger)
        {
            this.warmupService = warmupService;
            this.logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAsync()
        {
            bool completedValue = await this.warmupService.CompletedValueAsync();
            this.logger.Log(completedValue ? LogLevel.Debug : LogLevel.Error, $"CompletedValue:{completedValue} => [{string.Join(',', this.warmupService.HealthStatusProviders.Select(p => $"({p.GetType().GetFriendlyName()}, {p.IsHealthy})"))}]");

            if (!completedValue)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok();
        }
    }
}
