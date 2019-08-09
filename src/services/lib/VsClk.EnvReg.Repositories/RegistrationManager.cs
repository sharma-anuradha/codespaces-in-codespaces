using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsCloudKernel.Services.EnvReg.Repositories;
using Microsoft.VsSaaS.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VsClk.EnvReg.Models.DataStore.Compute;
using VsClk.EnvReg.Models.DataStore.Workspace;
using VsClk.EnvReg.Models.Errors;
using VsClk.EnvReg.Telemetry;

namespace VsClk.EnvReg.Repositories
{
    public class RegistrationManager : IRegistrationManager
    {
        private const int ENV_REG_QUOTA = 5;
        private const int PERSISTANT_SESSION_EXPIRES_IN_DAYS = 30;

        private AppSettings AppSettings { get; }
        private IEnvironmentRegistrationRepository EnvironmentRegistrationRepository { get; }
        private IComputeRepository ComputeRepository { get; }
        private IStorageManager FileShareManager { get; }
        private IWorkspaceRepository WorkspaceRepository { get; }

        public RegistrationManager(
            IEnvironmentRegistrationRepository environmentRegistrationRepository,
            IComputeRepository computeRepository,
            IStorageManager fileShareManager,
            IWorkspaceRepository workspaceRepository,
            AppSettings appSettings)
        {
            EnvironmentRegistrationRepository = environmentRegistrationRepository;
            ComputeRepository = computeRepository;
            FileShareManager = fileShareManager;
            WorkspaceRepository = workspaceRepository;
            AppSettings = appSettings;
        }

        public async Task<EnvironmentRegistration> GetAsync(string id, string ownerId, IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(id, nameof(id));
            var model = await EnvironmentRegistrationRepository.GetAsync(id, logger);
            if (model == null)
            {
                return null;
            }
            UnauthorizedUtil.IsTrue(model.OwnerId == ownerId);

            // Note: We do not do this in the case of GetListByOwnerAsync, because
            // Will require multiple calls to workspace service, causing un-necessary slowness and
            // No API as of now to pass multiple workspaceIds
            var workspace = await WorkspaceRepository.GetStatusAsync(model.Connection.ConnectionSessionId);
            if (workspace == null)
            {
                // In this case the workspace is deleted. There is no way of getting to an environment without it.
                model.State = StateInfo.Unavailable.ToString();
            }
            else
            {
                if (workspace.IsHostConnected.HasValue)
                {
                    model.State = workspace.IsHostConnected.Value ? StateInfo.Available.ToString() : StateInfo.Awaiting.ToString();
                }
                // else we don't change the model state.
            }

            return model;
        }

        public async Task<IEnumerable<EnvironmentRegistration>> GetListByOwnerAsync(string ownerId, IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(ownerId, nameof(ownerId));

            return await EnvironmentRegistrationRepository.GetWhereAsync((model) => model.OwnerId == ownerId, logger);
        }

        public async Task<EnvironmentRegistration> RegisterAsync(
            EnvironmentRegistration model,
            EnvironmentRegistrationOptions options,
            string ownerId,
            string accessToken,
            IDiagnosticsLogger logger)
        {
            // Setup
            model.OwnerId = ownerId;
            model.Created = DateTime.UtcNow;
            model.Updated = DateTime.UtcNow;
            model.Id = Guid.NewGuid().ToString();

            // Validation
            UnauthorizedUtil.IsRequired(accessToken);

            var environments = await EnvironmentRegistrationRepository.GetWhereAsync((env) => env.OwnerId == model.OwnerId, logger);

            ValidationUtil.IsTrue(
                !environments.Any((env) => env.FriendlyName == model.FriendlyName),
                "Environment with that friendlyName already exists");
            ValidationUtil.IsTrue(
                environments.Count() <= ENV_REG_QUOTA,
                "You already exceeded the quota of environments");

            // Action - If Static Environment
            if (model.Type.Equals(EnvType.StaticEnvironment.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                if (model.Connection == null)
                {
                    model.Connection = new ConnectionInfo();
                }

                if (string.IsNullOrWhiteSpace(model.Connection.ConnectionSessionId))
                {
                    model.Connection.ConnectionSessionId = await CreateWorkspace(model.Id, logger);
                }

                model.State = StateInfo.Provisioning.ToString();
                model = await EnvironmentRegistrationRepository.CreateAsync(model, logger);

                return model;
            }

            // Action - Create Workspace
            var sessionId = await CreateWorkspace(model.Id, logger);
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return null;
            }


            if (string.IsNullOrEmpty(model.ContainerImage))
            {
                // TODO: Hack to detect the container image. When we start supporting custom container images,
                // we will pass this from clients, and use appropriate logic to detect if it's kitchensink.
                model.ContainerImage = "kitchensink";
            }

            // Setup - Compute input
            var computeServiceRequest = new ComputeServiceRequest
            {
                EnvironmentVariables = EnvironmentVariableGenerator.Generate(model, AppSettings, accessToken, sessionId)
            };

            // Action - File Share
            FileShare fileShare = null;
            if (options.CreateFileShare)
            {
                fileShare = await FileShareManager.CreateFileShareForEnvironmentAsync(new FileShareEnvironmentInfo { FriendlyName = model.FriendlyName, OwnerId = model.OwnerId }, logger);
            }

            if (fileShare != null)
            {
                computeServiceRequest.Storage = new StorageSpecification
                {
                    // ComputeService doesn't know about environment file shares. It's able to mount them by name.
                    FileShareName = fileShare.Name
                };
                model.Storage = new StorageInfo { FileShareId = fileShare.Id };
            }

            // Action - Compute Service
            var computeTargets = await ComputeRepository.GetTargetsAsync();

            // Choose the right compute target to use based on availablity and region
            var serviceRegion = RegistrationUtils.StampToRegion(AppSettings.StampLocation);

            var computeTargetId = computeTargets
                .Where(c =>
                {
                    var isAvailable = c.State == "Available";
                    // Compute target region is okay if we haven't specified a region or the region matches the serviceRegion
                    var regionOk = serviceRegion == null || c.Properties.GetValueOrDefault("region") == serviceRegion;
                    return isAvailable && regionOk;
                })
                .FirstOrDefault()?.Id;

            if (string.IsNullOrEmpty(computeTargetId))
            {
                logger
                    .AddEnvironmentId(model.Id)
                    .AddSessionId(sessionId)
                    .AddOwnerId(model.OwnerId)
                    .LogError("provision_compute_resource_failed");

                return null;
            }

            // Create - Compute Resource
            var computeResource = await ComputeRepository.AddResourceAsync(computeTargetId, computeServiceRequest);
            var containerId = computeResource.Id;

            // Setup
            model.Connection = new ConnectionInfo()
            {
                ConnectionSessionId = sessionId,
                ConnectionComputeId = containerId,
                ConnectionComputeTargetId = computeTargetId
            };
            model.State = StateInfo.Provisioning.ToString();

            // Create - Environment Registration
            model = await EnvironmentRegistrationRepository.CreateAsync(model, logger);

            return model;
        }

        public async Task<bool> DeleteAsync(
            string id,
            string ownerId,
            IDiagnosticsLogger logger)
        {
            var model = await EnvironmentRegistrationRepository.GetAsync(id, logger);
            if (model == null)
            {
                return false;
            }
            UnauthorizedUtil.IsTrue(model.OwnerId == ownerId);

            if (model.Type.Equals(EnvType.CloudEnvironment.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                await ComputeRepository.DeleteResourceAsync(model.Connection.ConnectionComputeTargetId, model.Connection.ConnectionComputeId);
            }

            await WorkspaceRepository.DeleteAsync(model.Connection.ConnectionSessionId);
            await EnvironmentRegistrationRepository.DeleteAsync(id, logger);

            return true;
        }

        public async Task<EnvironmentRegistration> CallbackUpdateAsync(
            string id,
            EnvironmentRegistrationCallbackOptions options,
            string ownerId,
            IDiagnosticsLogger logger)
        {
            var model = await EnvironmentRegistrationRepository.GetAsync(id, logger);
            if (model == null)
            {
                return null;
            }

            UnauthorizedUtil.IsTrue(model.OwnerId == ownerId);
            ValidationUtil.IsTrue(model.Connection.ConnectionSessionId == options.Payload.SessionId);

            model.Connection.ConnectionSessionPath = options.Payload.SessionPath;
            model.State = StateInfo.Available.ToString();

            return await EnvironmentRegistrationRepository.UpdateAsync(model, logger);
        }

        private async Task<string> CreateWorkspace(
            string id,
            IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(id, nameof(id));
            var workspaceRequest = new WorkspaceRequest()
            {
                Name = id,
                ConnectionMode = ConnectionMode.Auto,
                AreAnonymousGuestsAllowed = false,
                ExpiresAt = DateTime.UtcNow.AddDays(PERSISTANT_SESSION_EXPIRES_IN_DAYS)
            };

            var workspaceResponse = await WorkspaceRepository.CreateAsync(workspaceRequest);
            if (string.IsNullOrWhiteSpace(workspaceResponse.Id))
            {
                logger
                    .AddEnvironmentId(id)
                    .LogError("workspace_creation_failed");
                return null;
            }

            return workspaceResponse.Id;
        }
    }
}
