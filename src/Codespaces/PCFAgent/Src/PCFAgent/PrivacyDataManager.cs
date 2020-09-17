// <copyright file="PrivacyDataManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.IdentityMap;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
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
        /// <param name="crossRegionActivator">Cross-region continuatinuation task activator.</param>
        /// <param name="superuserIdentity">Vso super user claims identity that has access to all environments and plans.</param>
        /// <param name="currentIdentityProvider">Current identity provider.</param>
        public PrivacyDataManager(
            IEnvironmentManager environmentManager,
            IIdentityMapRepository identityMapRepository,
            ICrossRegionContinuationTaskActivator crossRegionActivator,
            VsoSuperuserClaimsIdentity superuserIdentity,
            ICurrentIdentityProvider currentIdentityProvider)
        {
            EnvironmentManager = environmentManager;
            IdentityMapRepository = identityMapRepository;
            CrossRegionActivator = crossRegionActivator;
            SuperuserIdentity = superuserIdentity;
            CurrentIdentityProvider = currentIdentityProvider;
        }

        private IEnvironmentManager EnvironmentManager { get; }

        private IIdentityMapRepository IdentityMapRepository { get; }

        private ICrossRegionContinuationTaskActivator CrossRegionActivator { get; }

        private VsoSuperuserClaimsIdentity SuperuserIdentity { get; }

        private ICurrentIdentityProvider CurrentIdentityProvider { get; }

        /// <inheritdoc/>
        public async Task<int> DeleteEnvironmentsAsync(IEnumerable<CloudEnvironment> environments, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync("pcf_delete_environments", async (childLogger) =>
            {
                var affectedEntitiesCount = 0;
                foreach (var environment in environments)
                {
                    await QueueEnvironmentForDeletion(environment, logger.NewChildLogger());
                    affectedEntitiesCount++;
                }

                childLogger.AddBaseValue("PcfAffectedEntitiesCount", affectedEntitiesCount.ToString());
                return affectedEntitiesCount;
            });
        }

        /// <inheritdoc/>
        public async Task<int> DeleteUserIdentityMapAsync(IEnumerable<UserIdSet> userIdSets, IDiagnosticsLogger logger)
        {
            var affectedEntitiesCount = 0;

            await logger.OperationScopeAsync("pcf_delete_identitymap", async (childLogger) =>
            {
                foreach (var userIdSet in userIdSets)
                {
                    await Retry.DoAsync(async attempt =>
                    {
                        var map = await IdentityMapRepository.GetByUserIdSetAsync(userIdSet, logger);
                        if (map != null)
                        {
                            affectedEntitiesCount += await DeleteUserIdentityMap(map, childLogger);
                        }

                        childLogger.AddBaseValue("PcfAffectedEntitiesCount", affectedEntitiesCount.ToString());
                    });
                }
            });

            return affectedEntitiesCount;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> GetUserEnvironments(IEnumerable<UserIdSet> userIdSets, IDiagnosticsLogger logger)
        {
            using (CurrentIdentityProvider.SetScopedIdentity(SuperuserIdentity))
            {
                var allEnvironments = new List<CloudEnvironment>();
                foreach (var userIdSet in userIdSets)
                {
                    var map = await IdentityMapRepository.GetByUserIdSetAsync(userIdSet, logger);
                    var updatedUserIdSet = new UserIdSet(
                        map?.CanonicalUserId ?? userIdSet.CanonicalUserId,
                        map?.ProfileId ?? userIdSet.ProfileId,
                        map?.ProfileProviderId ?? userIdSet.ProfileProviderId);

                    var environments = await EnvironmentManager.ListAsync(null, null, updatedUserIdSet, EnvironmentListType.AllEnvironments, logger.NewChildLogger());
                    allEnvironments.AddRange(environments);
                }

                return allEnvironments;
            }
        }

        /// <inheritdoc/>
        public async Task<(int, JObject)> PerformExportAsync(UserIdSet userIdSet, IDiagnosticsLogger logger)
        {
            using (CurrentIdentityProvider.SetScopedIdentity(SuperuserIdentity))
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

                var environments = await EnvironmentManager.ListAsync(null, null, userIdSet, EnvironmentListType.AllEnvironments, logger.NewChildLogger());
                if (environments.Any())
                {
                    exportObject.Add("environments", CreateExport(environments));
                    affectedEntitiesCount += environments.Count();
                }

                return (affectedEntitiesCount, exportObject);
            }
        }

        private JToken CreateExport(object data)
        {
            return JToken.FromObject(data, new JsonSerializer { ContractResolver = new PcfExportContractResolver() });
        }

        private async Task<int> DeleteUserIdentityMap(IIdentityMapEntity map, IDiagnosticsLogger logger)
        {
            var isDeleted = await IdentityMapRepository.DeleteAsync(map.Id, logger);
            return isDeleted ? 1 : 0;
        }

        private async Task QueueEnvironmentForDeletion(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync("pcf_queue_environment_for_deletion", async (childLogger) =>
            {
                childLogger.AddCloudEnvironment(environment);
                var continuationInput = new EnvironmentContinuationInput { EnvironmentId = environment.Id };
                await CrossRegionActivator.ExecuteForDataPlane(SoftDeleteEnvironmentContinuationHandler.DefaultQueueTarget, environment.Location, continuationInput, logger);
            });
        }
    }
}
