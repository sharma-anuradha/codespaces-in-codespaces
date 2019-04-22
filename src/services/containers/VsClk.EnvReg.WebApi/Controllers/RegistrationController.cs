using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsCloudKernel.Services.EnvReg.Repositories;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.AspNetCore.Http;

namespace Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private IEnvironmentRegistrationRepository EnvironmentRegistrationRepository { get; }
        private IMapper Mapper { get; }
        private IConfiguration Configuration { get; }
        private AppSettings AppSettings { get; }

        public RegistrationController(
            IEnvironmentRegistrationRepository environmentRegistrationRepository,
            IMapper mapper,
            IConfiguration configuration,
            AppSettings appSettings)
        {
            EnvironmentRegistrationRepository = environmentRegistrationRepository;
            Mapper = mapper;
            Configuration = configuration;
            AppSettings = appSettings;
        }

        // GET api/environment/registration/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var logger = HttpContext.GetLogger();
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("'id' is required");
            }

            var modelRaw = await EnvironmentRegistrationRepository.GetAsync(id, logger);
            if (modelRaw == null)
            {
                return NotFound();
            }

            return Ok(Mapper.Map<EnvironmentRegistrationResult>(modelRaw));
        }

        // GET api/environment/registration
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var logger = HttpContext.GetLogger();
            var currentUserId = GetCurrentUserId();

            var modelsRaw = await EnvironmentRegistrationRepository.GetWhereAsync((model) => model.OwnerId == currentUserId, logger);
            if (modelsRaw == null)
            {
                return NotFound();
            }

            return Ok(Mapper.Map<EnvironmentRegistrationResult[]>(modelsRaw));
        }

        // POST api/environment/registration
        [HttpPost]
        public async Task<IActionResult> Post(
            [FromBody]EnvironmentRegistrationInput modelInput)
        {
            var logger = HttpContext.GetLogger();
            var currentUserId = GetCurrentUserId();

            if (modelInput == null)
            {
                return BadRequest();
            }

            var modelRaw = Mapper.Map<EnvironmentRegistration>(modelInput);
            modelRaw.Created = DateTime.UtcNow;
            modelRaw.Updated = DateTime.UtcNow;
            modelRaw.OwnerId = currentUserId;
            modelRaw.Id = Guid.NewGuid().ToString();

            modelRaw = await EnvironmentRegistrationRepository.CreateAsync(modelRaw, logger);

            return Ok(Mapper.Map<EnvironmentRegistrationResult>(modelRaw));
        }

        // PUT api/environment/registration/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(
            [FromRoute]string id, [FromBody]EnvironmentRegistrationInput modelInput)
        {
            var logger = HttpContext.GetLogger();
            var currentUserId = GetCurrentUserId();

            var modelRaw = await EnvironmentRegistrationRepository.GetAsync(id, logger);
            if (modelRaw == null)
            {
                return NotFound();
            }
            if (modelRaw.OwnerId != currentUserId)
            {
                return Unauthorized();
            }

            modelRaw.FriendlyName = modelInput.FriendlyName;

            modelRaw = await EnvironmentRegistrationRepository.UpdateAsync(modelRaw, logger);

            return Ok(Mapper.Map<EnvironmentRegistrationResult>(modelRaw));
        }

        // DELETE api/environment/registration/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var logger = HttpContext.GetLogger();
            var currentUserId = GetCurrentUserId();

            var modelRaw = await EnvironmentRegistrationRepository.GetAsync(id, logger);
            if (modelRaw == null)
            {
                return NotFound();
            }
            if (modelRaw.OwnerId != currentUserId)
            {
                return Unauthorized();
            }

            var deleted = await EnvironmentRegistrationRepository.DeleteAsync(id, logger);

            return Ok(Mapper.Map<EnvironmentRegistrationResult>(modelRaw));
        }


        private string GetCurrentUserId()
        {
            // Authenticated via AAD token
            var tokenUserId = HttpContext.GetCurrentUserId();
            if (tokenUserId != null) return tokenUserId;

            // Authenticated via cookie
            var idClaimType = "FullyQualifiedUserId";
            var ident = User.Identity as System.Security.Claims.ClaimsIdentity;
            var userID = ident.Claims.FirstOrDefault(c => c.Type == idClaimType)?.Value;
            return userID;
        }
    }
}
