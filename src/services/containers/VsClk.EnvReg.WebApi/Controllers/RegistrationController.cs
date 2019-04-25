using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        // GET api/environment/registration/<id>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEnvironment(string id)
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
        public async Task<IActionResult> GetList()
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
        public async Task<IActionResult> CreateEnvironment(
            [FromBody]EnvironmentRegistrationInput modelInput)
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<EnvironmentRegistrationInput, EnvironmentRegistration>();
                cfg.CreateMap<EnvironmentRegistration, EnvironmentRegistrationResult>();
            });
            var client = new HttpClient();
            var mapper = config.CreateMapper();
            var logger = HttpContext.GetLogger();
            var currentUserId = GetCurrentUserId();
            string accessToken = Util.Auth.GetAccessToken(Request);

            /* This should never happen when supporting getting the token from both the cookie and from jwt
             * Throwing 401 Unauthorized for now
             */
            if (string.IsNullOrEmpty(accessToken))
            {
                return StatusCode(401);
            }

            string apiUrl = Request.Scheme + "://" + Request.Host + Request.Path + "/";

            if (modelInput == null || !EnvironmentRegistrationInput.IsCreateInputValid(modelInput))
            {
                return BadRequest();
            }

            var duplicatedEnv = await EnvironmentRegistrationRepository.GetWhereAsync((model) => (model.FriendlyName == modelInput.FriendlyName & model.OwnerId == currentUserId), logger);
            if (duplicatedEnv.Any())
            {
                return BadRequest("Environment with that friendlyName already exists");
            }

            var modelRaw = mapper.Map<EnvironmentRegistrationInput, EnvironmentRegistration>(modelInput);
            modelRaw.Created = DateTime.UtcNow;
            modelRaw.Updated = DateTime.UtcNow;
            modelRaw.OwnerId = currentUserId;
            modelRaw.Id = Guid.NewGuid().ToString();
            var arguments = new List<KeyValuePair<string, string>>();

            /* Construct the arguments needed for the compute service */
            if (modelRaw.Seed != null && modelRaw.Seed.SeedType == "git" && EnvironmentRegistrationInput.IsValidGitUrl(modelRaw.Seed.SeedMoniker))
            {
                var moniker = modelRaw.Seed.SeedMoniker;

                /* Just supporting /pull/ case for now */
                if (moniker.Contains("/pull/"))
                {
                    var repoUrl = moniker.Split("/pull/");
                    arguments.Add(new KeyValuePair<string, string>("GIT_REPO_URL", repoUrl[0]));
                    arguments.Add(new KeyValuePair<string, string>("GIT_PR_NUM", Regex.Match(repoUrl[1], "(\\d+)").ToString()));
                }
                else
                {
                    arguments.Add(new KeyValuePair<string, string>("GIT_REPO_URL", moniker));
                }
            }

            arguments.Add(new KeyValuePair<string, string>("SESSION_CALLBACK", apiUrl + modelRaw.Id + "/_callback"));
            arguments.Add(new KeyValuePair<string, string>("SESSION_TOKEN", accessToken));
            var json = new Dictionary<string, List<KeyValuePair<string, string>>>()
            {
                { "environmentVariables", arguments }
            };
            var requestContent = new StringContent(JsonConvert.SerializeObject(json), Encoding.UTF8, "application/json");

            /* Call the compute service */
            var poolInfo = await client.GetAsync(AppSettings.ComputeServiceUrl + "/computeTargets");
            var computeTargetId = JArray.Parse(await poolInfo.Content.ReadAsStringAsync()).First["id"].ToString();
            if (!string.IsNullOrEmpty(computeTargetId))
            {
                /* This assumes that the compute service and this service are in the same host, is that not the case we should
                 * add the url to the App Settings
                 */
                var response = await client.PostAsync(AppSettings.ComputeServiceUrl + "/computeTargets/" + computeTargetId + "/compute", requestContent);
                var contents = await response.Content.ReadAsStringAsync();
                string containerId = JObject.Parse(contents)["id"].ToString();
                modelRaw.Connection = new ConnectionInfo();
                modelRaw.Connection.ConnectionComputeId = containerId;
                modelRaw.Connection.ConnectionComputeTargetId = computeTargetId;
                modelRaw.State = StateInfo.Provisioning.ToString();
                modelRaw = await EnvironmentRegistrationRepository.CreateAsync(modelRaw, logger);
                return Ok(mapper.Map<EnvironmentRegistration, EnvironmentRegistrationResult>(modelRaw));

            }
            else
            {
                return StatusCode(409);
            }


        }

        // PUT api/environment/registration/<id>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(
            [FromRoute]string id, [FromBody]EnvironmentRegistrationInput modelInput)
        {
            return StatusCode(501);
        }

        // DELETE api/environment/registration/<id>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var client = new HttpClient();
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
            var response = await client.DeleteAsync(AppSettings.ComputeServiceUrl + "/computeTargets/" + modelRaw.Connection.ConnectionComputeTargetId + "/compute/" + modelRaw.Connection.ConnectionComputeId);
            var deleted = await EnvironmentRegistrationRepository.DeleteAsync(id, logger);

            return NoContent();
        }

        // PATCH api/environment/registration/<id>
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id)
        {
            return StatusCode(501);
        }

        // GET api/environment/registration/<id>/tasks/<taskId>
        [HttpGet("{id}/tasks/{taskId}")]
        public async Task<IActionResult> GetTask(string id, string taskId)
        {
            return StatusCode(501);
        }

        // POST  api/environment/registration/<id>/_callback
        [HttpPost("{id}/_callback")]
        public async Task<IActionResult> Callback(
            string id)
        {
            string rawJson;
            var logger = HttpContext.GetLogger();
            var currentUserId = GetCurrentUserId();
            using (var reader = new StreamReader(Request.Body))
            {
                rawJson = reader.ReadToEnd();
            }
            var inputJson = JObject.Parse(rawJson);
            var input = JsonConvert.DeserializeObject<ConnectionInfo>(inputJson["payload"].ToString());

            var modelRaw = await EnvironmentRegistrationRepository.GetAsync(id, logger);
            if (modelRaw == null)
            {
                return NotFound();
            }

            modelRaw.Connection.ConnectionSessionId = input.ConnectionSessionId;
            modelRaw.Connection.ConnectionSessionPath = input.ConnectionSessionPath;
            modelRaw.State = StateInfo.Available.ToString();
            modelRaw = await EnvironmentRegistrationRepository.UpdateAsync(modelRaw, logger);
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
