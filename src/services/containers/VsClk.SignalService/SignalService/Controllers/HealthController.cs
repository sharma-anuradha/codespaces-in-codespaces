using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService.Controllers
{
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {

        private readonly IList<IHealthStatusProvider> healthStatusProviders;

        public HealthController(
            IList<IHealthStatusProvider> healthStatusProviders)
        {
            this.healthStatusProviders = healthStatusProviders;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetAsync()
        {
            if (this.healthStatusProviders.FirstOrDefault(ws => !ws.State) != null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok();
        }

    }
}
