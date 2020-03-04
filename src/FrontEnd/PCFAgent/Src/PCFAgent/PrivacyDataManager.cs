// <copyright file="PrivacyDataManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.IdentityMap;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PCFAgent
{
    /// <summary>
    /// Privacy Data Manager implementation.
    /// </summary>
    public class PrivacyDataManager : IPrivacyDataManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrivacyDataManager"/> class.
        /// </summary>
        /// <param name="environmentManager">Environment manager.</param>
        /// <param name="identityMapRepository">The identity map repository.</param>
        public PrivacyDataManager(IEnvironmentManager environmentManager, IIdentityMapRepository identityMapRepository)
        {
            EnvironmentManager = environmentManager;
            IdentityMapRepository = identityMapRepository;
        }

        private IEnvironmentManager EnvironmentManager { get; }

        private IIdentityMapRepository IdentityMapRepository { get; }

        /// <inheritdoc/>
        public async Task<int> PerformDeleteAsync(UserIdSet userIdSet, IDiagnosticsLogger logger)
        {
            var affectedEntitiesCount = 0;
            await logger.OperationScopeAsync("pcf_perform_delete", async (childLogger) =>
            {
                var map = await IdentityMapRepository.GetByUserIdSetAsync(userIdSet, logger);
                userIdSet = new UserIdSet(
                    map?.CanonicalUserId ?? userIdSet.CanonicalUserId,
                    map?.ProfileId ?? userIdSet.ProfileId,
                    map?.ProfileProviderId ?? userIdSet.ProfileProviderId);

                affectedEntitiesCount += await DeleteUserEnvironments(userIdSet, childLogger);
                if (map != null)
                {
                    affectedEntitiesCount += await DeleteUserIdentityMap(map, childLogger);
                }

                childLogger.AddBaseValue("PcfAffectedEntitiesCount", affectedEntitiesCount.ToString());
            });
            return affectedEntitiesCount;
        }

        /// <inheritdoc/>
        public async Task<(int, JObject)> PerformExportAsync(UserIdSet userIdSet, IDiagnosticsLogger logger)
        {
            var affectedEntitiesCount = 0;
            var exportObject = new JObject();
            var map = await IdentityMapRepository.GetByUserIdSetAsync(userIdSet, logger);
            if (map != null)
            {
                userIdSet = new UserIdSet(map.CanonicalUserId ?? userIdSet.CanonicalUserId, map.ProfileId ?? userIdSet.CanonicalUserId, map.ProfileProviderId ?? userIdSet.CanonicalUserId);
                exportObject.Add("identityMap", CreateExport(map));
                affectedEntitiesCount += 1;
            }

            var environments = await EnvironmentManager.ListAsync(logger: logger.NewChildLogger(), userIdSet: userIdSet);
            if (environments.Any())
            {
                exportObject.Add("environments", CreateExport(environments));
                affectedEntitiesCount += environments.Count();
            }

            return (affectedEntitiesCount, exportObject);
        }

        private JToken CreateExport(object data)
        {
            return JToken.FromObject(data, new JsonSerializer { ContractResolver = new PcfExportContractResolver() });
        }

        private async Task<int> DeleteUserEnvironments(UserIdSet userIdSet, IDiagnosticsLogger logger)
        {
            var environments = await EnvironmentManager.ListAsync(logger: logger.NewChildLogger(), userIdSet: userIdSet);
            var recordsDeleted = 0;
            foreach (var environment in environments)
            {
                await EnvironmentManager.DeleteAsync(environment, logger);
                recordsDeleted++;
            }

            return recordsDeleted;
        }

        private async Task<int> DeleteUserIdentityMap(IIdentityMapEntity map, IDiagnosticsLogger logger)
        {
            var isDeleted = await IdentityMapRepository.DeleteAsync(map.Id, logger);
            return isDeleted ? 1 : 0;
        }
    }
}
