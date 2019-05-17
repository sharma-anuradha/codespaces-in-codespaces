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
using Microsoft.VsCloudKernel.Services.EnvReg.WebApi.Models;
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
        private IStorageManager  FileShareManager { get; }
        private AppSettings AppSettings { get; }

        public RegistrationController(
            IEnvironmentRegistrationRepository environmentRegistrationRepository,
            IMapper mapper,
            IConfiguration configuration,
            IStorageManager  fileShareManager,
            AppSettings appSettings)
        {
            EnvironmentRegistrationRepository = environmentRegistrationRepository;
            Mapper = mapper;
            Configuration = configuration;
            FileShareManager = fileShareManager;
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

        public async Task<IActionResult> CreateStaticEnvironment(EnvironmentRegistration modelRaw)
        {
            var logger = HttpContext.GetLogger();

            modelRaw.State = StateInfo.Available.ToString();
            modelRaw = await EnvironmentRegistrationRepository.CreateAsync(modelRaw, logger);
            return Ok(Mapper.Map<EnvironmentRegistration, EnvironmentRegistrationResult>(modelRaw));
        }

        // POST api/environment/registration
        [HttpPost]
        public async Task<IActionResult> CreateEnvironment(
            [FromBody]EnvironmentRegistrationInput modelInput)
        {
            var client = new HttpClient();
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

            if (modelInput == null || !Util.Utils.IsCreateInputValid(modelInput))
            {
                return BadRequest();
            }

            var environments = await EnvironmentRegistrationRepository.GetWhereAsync((model) => model.OwnerId == currentUserId, logger);

            if (environments.Where((model) => (model.FriendlyName == modelInput.FriendlyName)).Any())
            {
                return BadRequest("Environment with that friendlyName already exists");
            }

            if (environments.Count() >= 10)
            {
                return BadRequest("You already exceeded the quota of environments");
            }

            var modelRaw = Mapper.Map<EnvironmentRegistrationInput, EnvironmentRegistration>(modelInput);
            modelRaw.Created = DateTime.UtcNow;
            modelRaw.Updated = DateTime.UtcNow;
            modelRaw.OwnerId = currentUserId;
            modelRaw.Id = Guid.NewGuid().ToString();
            var environmentVariables = new List<EnvironmentVariable>();

            if (modelRaw.Type == EnvType.staticEnvironment.ToString())
            {
                return await CreateStaticEnvironment(modelRaw);
            }

            /* Construct the arguments needed for the compute service */
            if (modelRaw.Seed != null
                && modelRaw.Seed.SeedType == "git"
                && Util.Utils.IsValidGitUrl(modelRaw.Seed.SeedMoniker))
            {
                var moniker = modelRaw.Seed.SeedMoniker;

                /* Just supporting /pull/ case for now */
                if (moniker.Contains("/pull/"))
                {
                    var repoUrl = moniker.Split("/pull/");
                    environmentVariables.Add(new EnvironmentVariable("GIT_REPO_URL", repoUrl[0]));
                    environmentVariables.Add(new EnvironmentVariable("GIT_PR_NUM", Regex.Match(repoUrl[1], "(\\d+)").ToString()));
                }
                else
                {
                    environmentVariables.Add(new EnvironmentVariable("GIT_REPO_URL", moniker));
                }
            }

            string apiUrl = AppSettings.PreferredSchema + "://" + AppSettings.DefaultHost + AppSettings.DefaultPath + "/registration/";
            environmentVariables.Add(new EnvironmentVariable("SESSION_CALLBACK", apiUrl + modelRaw.Id + "/_callback"));
            environmentVariables.Add(new EnvironmentVariable("SESSION_TOKEN", accessToken));
            
            var computeServiceInput = new ComputeServiceInput
            {
                EnvironmentVariables = environmentVariables
            };

            var fileShare = await FileShareManager.CreateFileShareForEnvironmentAsync(new FileShareEnvironmentInfo { FriendlyName = modelRaw.FriendlyName, OwnerId = modelRaw.OwnerId }, logger);
            if (fileShare != null)
            {
                computeServiceInput.Storage = new StorageSpecification
                {
                    // ComputeService doesn't know about environment file shares. It's able to mount them by name.
                    FileShareName = fileShare.Name
                };

                modelRaw.Storage = new StorageInfo { FileShareId = fileShare.Id };
            }

            var requestContent = new StringContent(JsonConvert.SerializeObject(computeServiceInput), Encoding.UTF8, "application/json");

            var computeTargetsUri = new Uri(AppSettings.ComputeServiceUri, "/computeTargets");
            /* Call the compute service */
            var poolInfo = await client.GetAsync(computeTargetsUri);
            var content = await poolInfo.Content.ReadAsStringAsync();
            var computeTargetId = JArray.Parse(content).First["id"].ToString();
            if (!string.IsNullOrEmpty(computeTargetId))
            {
                /* This assumes that the compute service and this service are in the same host, is that not the case we should
                 * add the url to the App Settings
                 */
                var computeCreationUri = new Uri(AppSettings.ComputeServiceUri, string.Format("/computeTargets/{0}/compute", computeTargetId));
                var response = await client.PostAsync(computeCreationUri, requestContent);
                var contents = await response.Content.ReadAsStringAsync();
                string containerId = JObject.Parse(contents)["id"].ToString();
                modelRaw.Connection = new ConnectionInfo();
                modelRaw.Connection.ConnectionComputeId = containerId;
                modelRaw.Connection.ConnectionComputeTargetId = computeTargetId;
                modelRaw.State = StateInfo.Provisioning.ToString();
                modelRaw = await EnvironmentRegistrationRepository.CreateAsync(modelRaw, logger);
                return Ok(Mapper.Map<EnvironmentRegistration, EnvironmentRegistrationResult>(modelRaw));
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

            if (modelRaw.Type == EnvType.cloudEnvironment.ToString())
            {
                await client.DeleteAsync(AppSettings.ComputeServiceUrl + "/computeTargets/" + modelRaw.Connection.ConnectionComputeTargetId + "/compute/" + modelRaw.Connection.ConnectionComputeId);
            }

            await EnvironmentRegistrationRepository.DeleteAsync(id, logger);
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
