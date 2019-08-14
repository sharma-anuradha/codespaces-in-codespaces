using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Middleware;
using Microsoft.VsCloudKernel.Services.Logging;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using System;
using System.Threading.Tasks;
using VsClk.EnvReg.Models.Errors;
using VsClk.EnvReg.Repositories;
using VsClk.EnvReg.Telemetry;

namespace Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Controllers
{
    [FriendlyExceptionFilter]
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private IRegistrationManager RegistrationManager { get; }
        private ICurrentUserProvider CurrentUserProvider { get; }
        private IMapper Mapper { get; }

        public RegistrationController(
            IRegistrationManager registrationManager,
            ICurrentUserProvider currentUserProvider,
            IMapper mapper)
        {
            RegistrationManager = registrationManager;
            CurrentUserProvider = currentUserProvider;
            Mapper = mapper;
        }

        // GET api/environment/registration/<id>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEnvironment(string id)
        {
            var logger = HttpContext.GetLogger();
            var currentUserId = CurrentUserProvider.GetProfileId();

            var result = await RegistrationManager.GetAsync(id, currentUserId, logger);
            if (result == null)
            {
                return NotFound();
            }

            logger.AddRegistrationInfoToResponseLog(result);
            return Ok(Mapper.Map<EnvironmentRegistrationResult>(result));
        }

        // GET api/environment/registration
        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var logger = HttpContext.GetLogger();
            var currentUserId = CurrentUserProvider.GetProfileId();

            var modelsRaw = await RegistrationManager.GetListByOwnerAsync(currentUserId, logger);
            if (modelsRaw == null)
            {
                return NotFound();
            }

            return Ok(Mapper.Map<EnvironmentRegistrationResult[]>(modelsRaw));
        }

        // POST api/environment/registration
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] EnvironmentRegistrationInput modelInput)
        {
            var logger = HttpContext.GetLogger();
            var currentUserId = CurrentUserProvider.GetProfileId();
            var accessToken = CurrentUserProvider.GetBearerToken();

            ValidationUtil.IsRequired(modelInput);
            ValidationUtil.IsRequired(modelInput.FriendlyName);
            ValidationUtil.IsRequired(modelInput.Type);

            var model = Mapper.Map<EnvironmentRegistrationInput, EnvironmentRegistration>(modelInput);

            model = await RegistrationManager.RegisterAsync(
                model,
                new EnvironmentRegistrationOptions { CreateFileShare = modelInput.CreateFileShare },
                currentUserId,
                accessToken,
                logger);

            if (model != null)
            {
                logger.AddRegistrationInfoToResponseLog(model);
                return Ok(Mapper.Map<EnvironmentRegistration, EnvironmentRegistrationResult>(model));
            }

            return StatusCode(409);
        }

        // PUT api/environment/registration/<id>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(
            [FromRoute]string id, [FromBody]EnvironmentRegistrationInput modelInput)
        {
#if DEBUG
            ValidationUtil.IsRequired(id);

            var logger = HttpContext.GetLogger();
            var currentUserId = CurrentUserProvider.GetProfileId();
            var accessToken = CurrentUserProvider.GetBearerToken();

            ValidationUtil.IsRequired(modelInput);
            ValidationUtil.IsRequired(modelInput.FriendlyName);
            ValidationUtil.IsRequired(modelInput.Type);

            var model = Mapper.Map<EnvironmentRegistrationInput, EnvironmentRegistration>(modelInput);

            model = await RegistrationManager.RefreshAsync(
                id, 
                model, 
                null, 
                currentUserId, 
                accessToken, 
                logger);

            if (model != null)
            {
                logger.AddRegistrationInfoToResponseLog(model);
                return Ok(Mapper.Map<EnvironmentRegistration, EnvironmentRegistrationResult>(model));
            }

            return StatusCode(409);
#else
            return StatusCode(501);
#endif
        }

        // DELETE api/environment/registration/<id>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            [FromRoute]string id)
        {
            var logger = HttpContext.GetLogger();
            var currentUserId = CurrentUserProvider.GetProfileId();

            logger.AddEnvironmentId(id);

            var result = await RegistrationManager.DeleteAsync(
                id,
                currentUserId,
                logger);

            if (!result)
            {
                logger.LogWarning("env_not_found");
                return NotFound();
            }

            return NoContent();
        }

        // PATCH api/environment/registration/<id>
        [HttpPatch("{id}")]
        public Task<IActionResult> Patch(
            [FromRoute]string id)
        {
            return Task.FromResult<IActionResult>(StatusCode(501));
        }

        // GET api/environment/registration/<id>/tasks/<taskId>
        [HttpGet("{id}/tasks/{taskId}")]
        public Task<IActionResult> GetTask(
            [FromRoute]string id, [FromRoute]string taskId)
        {
            return Task.FromResult<IActionResult>(StatusCode(501));
        }

        // POST  api/environment/registration/<id>/_callback
        [HttpPost("{id}/_callback")]
        public async Task<IActionResult> Callback(
            [FromRoute]string id, [FromBody]EnvironmentRegistrationCallbackInput modelInput)
        {
            var logger = HttpContext.GetLogger();
            var currentUserId = CurrentUserProvider.GetProfileId();

            var options = Mapper.Map<EnvironmentRegistrationCallbackInput, EnvironmentRegistrationCallbackOptions>(modelInput);

            var result = await RegistrationManager.CallbackUpdateAsync(
                id,
                options,
                currentUserId,
                logger);

            if (result == null)
            {
                logger
                    .AddEnvironmentId(id)
                    .AddSessionId(modelInput.Payload.SessionId)
                    .LogError("env_not_found_on_callback");
                return NotFound();
            }

            logger.AddRegistrationInfoToResponseLog(result);
            return Ok(Mapper.Map<EnvironmentRegistrationResult>(result));
        }
    }
}
