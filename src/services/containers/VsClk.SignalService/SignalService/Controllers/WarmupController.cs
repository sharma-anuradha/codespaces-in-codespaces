using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Common.Warmup;

namespace Microsoft.VsCloudKernel.SignalService.Controllers
{
    [Route("[controller]")]
    public class WarmupController : ControllerBase
    {
        private readonly IList<IAsyncWarmup> warmupServices;

        public WarmupController(IList<IAsyncWarmup> warmupServices)
        {
            this.warmupServices = warmupServices;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAsync()
        {
            await WarmupUtility.WhenAllWarmupCompletedAsync(this.warmupServices);
            if (this.warmupServices.OfType<IHealthStatusProvider>().FirstOrDefault(ws => !ws.State) != null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok();
        }
    }
}
