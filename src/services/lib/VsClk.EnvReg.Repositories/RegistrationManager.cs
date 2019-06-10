﻿using Microsoft.VsCloudKernel.Services.EnvReg.Models;
using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsCloudKernel.Services.EnvReg.Repositories;
using Microsoft.VsSaaS.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VsClk.EnvReg.Models.DataStore.Compute;
using VsClk.EnvReg.Models.Errors;

namespace VsClk.EnvReg.Repositories
{
    public class RegistrationManager : IRegistrationManager
    {
        private const int ENV_REG_QUOTA = 5;

        private AppSettings AppSettings { get; }
        private IEnvironmentRegistrationRepository EnvironmentRegistrationRepository { get; }
        private IComputeRepository ComputeRepository { get; }
        private IStorageManager FileShareManager { get; }

        public RegistrationManager(
            IEnvironmentRegistrationRepository environmentRegistrationRepository,
            IComputeRepository computeRepository,
            IStorageManager fileShareManager,
            AppSettings appSettings)
        {
            EnvironmentRegistrationRepository = environmentRegistrationRepository;
            ComputeRepository = computeRepository;
            FileShareManager = fileShareManager;
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
            if (model.Type == EnvType.StaticEnvironment.ToString())
            {
                model.State = StateInfo.Available.ToString();
                model = await EnvironmentRegistrationRepository.CreateAsync(model, logger);

                return model;
            }

            // Setup - Compute input
            var computeServiceRequest = new ComputeServiceRequest
            {
                EnvironmentVariables = EnvironmentVariableGenerator.Generate(model, AppSettings, accessToken)
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
            var computeTargets = await ComputeRepository.GetTargets();
            var computeTargetId = computeTargets.FirstOrDefault()?.Id;
            if (!string.IsNullOrEmpty(computeTargetId))
            {
                // Create - Compute Resource
                var computeResource = await ComputeRepository.AddResource(computeTargetId, computeServiceRequest);
                var containerId = computeResource.Id;

                // Setup
                model.Connection = new ConnectionInfo
                {
                    ConnectionComputeId = containerId,
                    ConnectionComputeTargetId = computeTargetId
                };
                model.State = StateInfo.Provisioning.ToString();

                // Create - Environment Registration
                model = await EnvironmentRegistrationRepository.CreateAsync(model, logger);
                
                return model;
            }

            return null;
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

            if (model.Type == EnvType.CloudEnvironment.ToString())
            {
                await ComputeRepository.DeleteResource(model.Connection.ConnectionComputeTargetId, model.Connection.ConnectionComputeId);
            }

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

            model.Connection.ConnectionSessionId = options.Payload.SessionId;
            model.Connection.ConnectionSessionPath = options.Payload.SessionPath;
            model.State = StateInfo.Available.ToString();

            return await EnvironmentRegistrationRepository.UpdateAsync(model, logger);
        }
    }
}
