using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.VsCloudKernel.SignalService.Controllers
{
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {

        private readonly HealthService healthService;

        public HealthController(
            HealthService healthService)
        {
            this.healthService = healthService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetAsync()
        {
            if (!this.healthService.State)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok();
        }

    }
}
