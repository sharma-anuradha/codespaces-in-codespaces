// <copyright file="CloudEnvironmentRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository
{
    /// <summary>
    /// Manages the Global and Regional CloudEnvironment repositories.
    /// </summary>
    public class CloudEnvironmentRepository : ICloudEnvironmentRepository
    {
        private RolloutStatus rolloutStatus = RolloutStatus.Phase1;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentRepository"/> class.
        /// </summary>
        /// <param name="globalRepository">The global cloud environment repository.</param>
        /// <param name="regionalRepository">The regional cloud environment repository.</param>
        public CloudEnvironmentRepository(
            IGlobalCloudEnvironmentRepository globalRepository,
            IRegionalCloudEnvironmentRepository regionalRepository)
        {
            GlobalRepository = Requires.NotNull(globalRepository, nameof(globalRepository));
            RegionalRepository = Requires.NotNull(regionalRepository, nameof(regionalRepository));
        }

        private enum RolloutStatus
        {
            /// <summary>
            /// During this phase, environment records will be migrated from the GlobalRepository over to the RegionalRepositories.
            /// Any environment that exists in the RegionalRepository is considered the official record and so GlobalRepository
            /// records are only used if/when they do not (yet) exist in the RegionalRepository.
            /// </summary>
            Phase1,

            /// <summary>
            /// During this phase, all environments are assumed to have been migrated to the RegionalRepositories but the GlobalRepository
            /// still exists because it is needed by the EnvironmentsController in order to do HTTP redirection to the appropriate region.
            /// CloudEnvironments that get created in the GlobalRepository will only contain the minimal subset of information needed by
            /// the EnvironmentsController to do access checks and HTTP redirection.
            /// </summary>
            Phase2,

            /// <summary>
            /// This is the final phase. By this point, the need for the GlobalRepository has been eliminated by the switch-over to Cascade
            /// tokens which contain the region location information thereby eliminating the need for GlobalRepository lookups.
            /// </summary>
            Phase3,
        }

        /// <inheritdoc/>
        public IGlobalCloudEnvironmentRepository GlobalRepository { get; }

        /// <inheritdoc/>
        public IRegionalCloudEnvironmentRepository RegionalRepository { get; }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> CreateAsync(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            switch (rolloutStatus)
            {
                case RolloutStatus.Phase1:
                    // Create the record in the global repository first so that a key id will be assigned that we can be sure
                    // won't conflict later. If we created it in the regional db first, then there is a (small?) chance that
                    // when we later went to add it to the global repository, the id would clash (i.e. key might be unique
                    // in the regional db, but not unique in the global db).
                    environment = await GlobalRepository.CreateAsync(environment, logger.NewChildLogger());

                    try
                    {
                        // Now attempt to create the record in the regional db (if this fails, it's ok since List/Get/etc will
                        // continue to work and the migration task will eventually pick this up and migrate it.
                        environment.IsMigrated = true;

                        environment = await RegionalRepository.CreateAsync(environment, logger.NewChildLogger());
                    }
                    catch
                    {
                        environment.IsMigrated = false;

                        return environment;
                    }

                    try
                    {
                        // Update the record to specify that this has been migrated.
                        return await GlobalRepository.UpdateAsync(environment, logger.NewChildLogger());
                    }
                    catch
                    {
                        // Don't worry, the migration task will handle this.
                        return environment;
                    }

                case RolloutStatus.Phase2:
                    // Create the record in the global repository first so that a key id will be assigned that we can be sure
                    // won't conflict later. If we created it in the regional db first, then there is a (small?) chance that
                    // when we later went to add it to the global repository, the id would clash (i.e. key might be unique
                    // in the regional db, but not unique in the global db).
                    var globalEnvironment = await GlobalRepository.CreateAsync(CreateGlobalEnvironment(environment), logger.NewChildLogger());

                    try
                    {
                        // Now attempt to create the record in the regional db using the Id generated by the GlobalRepository.
                        environment.Id = globalEnvironment.Id;

                        return await RegionalRepository.CreateAsync(environment, logger.NewChildLogger());
                    }
                    catch
                    {
                        await GlobalRepository.DeleteAsync(globalEnvironment.Id, logger.NewChildLogger());
                        throw;
                    }

                default:
                    return await RegionalRepository.CreateAsync(environment, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(DocumentDbKey id, IDiagnosticsLogger logger)
        {
            switch (rolloutStatus)
            {
                case RolloutStatus.Phase1:
                    // Note: We delete environments from the repositories in the same order that they were created in otherwise
                    // the migration worker task might restore the environment.
                    await GlobalRepository.DeleteAsync(id, logger.NewChildLogger());

                    return await logger.RetryOperationScopeAsync(
                        "cloud_environment_repository_delete_retry_scope",
                        async (retryLogger) =>
                        {
                            return await RegionalRepository.DeleteAsync(id, retryLogger);
                        });
                case RolloutStatus.Phase2:
                    // We don't care if this is successful or not since nothing other than redirection uses it for anything.
                    // It's mostly just about cleanup.
                    try
                    {
                        await GlobalRepository.DeleteAsync(id, logger.NewChildLogger());
                    }
                    catch
                    {
                    }

                    return await RegionalRepository.DeleteAsync(id, logger.NewChildLogger());
                default:
                    return await RegionalRepository.DeleteAsync(id, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> GetAsync(DocumentDbKey id, IDiagnosticsLogger logger)
        {
            if (rolloutStatus == RolloutStatus.Phase1)
            {
                var environment = await RegionalRepository.GetAsync(id, logger.NewChildLogger());

                // This returns the global repository's version of the CloudEnvironment if it doesn't (yet?) exist in the regional repository since it may have just not been migrated yet.
                return environment ?? await GlobalRepository.GetAsync(id, logger.NewChildLogger());
            }
            else
            {
                return await RegionalRepository.GetAsync(id, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> ListAsync(string planId, IDiagnosticsLogger logger)
        {
            Expression<Func<CloudEnvironment, bool>> where = (cloudEnvironment) => cloudEnvironment.PlanId == planId;

            if (rolloutStatus == RolloutStatus.Phase1)
            {
                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                var globalEnvironments = await GlobalRepository.GetWhereAsync(where, logger.NewChildLogger());
                var regionalEnvironments = await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());

                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> ListAsync(string planId, string friendlyNameInLowerCase, IDiagnosticsLogger logger)
        {
            Expression<Func<CloudEnvironment, bool>> where = (cloudEnvironment) => cloudEnvironment.PlanId == planId && cloudEnvironment.FriendlyNameInLowerCase == friendlyNameInLowerCase;

            if (rolloutStatus == RolloutStatus.Phase1)
            {
                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                var globalEnvironments = await GlobalRepository.GetWhereAsync(where, logger.NewChildLogger());
                var regionalEnvironments = await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());

                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> ListAsync(UserIdSet userIdSet, IDiagnosticsLogger logger)
        {
            Expression<Func<CloudEnvironment, bool>> where = (cloudEnvironment) => (cloudEnvironment.OwnerId == userIdSet.CanonicalUserId ||
                cloudEnvironment.OwnerId == userIdSet.ProfileId);

            if (rolloutStatus == RolloutStatus.Phase1)
            {
                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                var globalEnvironments = await GlobalRepository.GetWhereAsync(where, logger.NewChildLogger());
                var regionalEnvironments = await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());

                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> ListAsync(UserIdSet userIdSet, string friendlyNameInLowerCase, IDiagnosticsLogger logger)
        {
            Expression<Func<CloudEnvironment, bool>> where = (cloudEnvironment) => (cloudEnvironment.OwnerId == userIdSet.CanonicalUserId ||
                cloudEnvironment.OwnerId == userIdSet.ProfileId) && cloudEnvironment.FriendlyNameInLowerCase == friendlyNameInLowerCase;

            if (rolloutStatus == RolloutStatus.Phase1)
            {
                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                var globalEnvironments = await GlobalRepository.GetWhereAsync(where, logger.NewChildLogger());
                var regionalEnvironments = await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());

                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> ListAsync(string planId, UserIdSet userIdSet, IDiagnosticsLogger logger)
        {
            Expression<Func<CloudEnvironment, bool>> where = (cloudEnvironment) => (cloudEnvironment.OwnerId == userIdSet.CanonicalUserId ||
                cloudEnvironment.OwnerId == userIdSet.ProfileId) && cloudEnvironment.PlanId == planId;

            if (rolloutStatus == RolloutStatus.Phase1)
            {
                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                var globalEnvironments = await GlobalRepository.GetWhereAsync(where, logger.NewChildLogger());
                var regionalEnvironments = await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());

                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> ListAsync(string planId, UserIdSet userIdSet, string friendlyNameInLowerCase, IDiagnosticsLogger logger)
        {
            Expression<Func<CloudEnvironment, bool>> where = (cloudEnvironment) => (cloudEnvironment.OwnerId == userIdSet.CanonicalUserId ||
                cloudEnvironment.OwnerId == userIdSet.ProfileId) && cloudEnvironment.PlanId == planId &&
                cloudEnvironment.FriendlyNameInLowerCase == friendlyNameInLowerCase;

            if (rolloutStatus == RolloutStatus.Phase1)
            {
                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                var globalEnvironments = await GlobalRepository.GetWhereAsync(where, logger.NewChildLogger());
                var regionalEnvironments = await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());

                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> UpdateAsync(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            if (rolloutStatus == RolloutStatus.Phase1)
            {
                environment.IsMigrated = true;

                // Note: We use CreateOrUpdate for the RegionalRepository because it might not have been migrated over to the regional db yet.
                environment = await RegionalRepository.CreateOrUpdateAsync(environment, logger.NewChildLogger());

                // Update the record in the global repository as well so that the Get*Count() methods continue to work.
                var globalEnvironment = await logger.RetryOperationScopeAsync(
                       "cloud_environment_repository_update_getglobal_retry_scope",
                       async (retryLogger) =>
                       {
                           return await GlobalRepository.GetAsync(environment.Id, logger.NewChildLogger());
                       });

                CopyCloudEnvironment(environment, globalEnvironment);

                await logger.RetryOperationScopeAsync(
                       "cloud_environment_repository_update_global_retry_scope",
                       async (retryLogger) =>
                       {
                           return await GlobalRepository.UpdateAsync(globalEnvironment, retryLogger);
                       });

                return environment;
            }
            else
            {
                return await RegionalRepository.UpdateAsync(environment, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> GetAllEnvironmentsInSubscriptionAsync(string subscriptionId, IDiagnosticsLogger logger)
        {
            if (rolloutStatus == RolloutStatus.Phase1)
            {
                var regionalEnvironments = await RegionalRepository.GetAllEnvironmentsInSubscriptionAsync(subscriptionId, logger.NewChildLogger());
                var globalEnvironments = await GlobalRepository.GetAllEnvironmentsInSubscriptionAsync(subscriptionId, logger.NewChildLogger());

                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await RegionalRepository.GetAllEnvironmentsInSubscriptionAsync(subscriptionId, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public Task<int> GetCloudEnvironmentPlanCountAsync(IDiagnosticsLogger logger)
        {
            if (rolloutStatus == RolloutStatus.Phase1)
            {
                return GlobalRepository.GetCloudEnvironmentPlanCountAsync(logger.NewChildLogger());
            }
            else
            {
                return RegionalRepository.GetCloudEnvironmentPlanCountAsync(logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public Task<int> GetCloudEnvironmentSubscriptionCountAsync(IDiagnosticsLogger logger)
        {
            if (rolloutStatus == RolloutStatus.Phase1)
            {
                return GlobalRepository.GetCloudEnvironmentSubscriptionCountAsync(logger.NewChildLogger());
            }
            else
            {
                return RegionalRepository.GetCloudEnvironmentSubscriptionCountAsync(logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public Task<int> GetEnvironmentsArchiveJobActiveCountAsync(IDiagnosticsLogger logger)
        {
            if (rolloutStatus == RolloutStatus.Phase1)
            {
                return GlobalRepository.GetEnvironmentsArchiveJobActiveCountAsync(logger.NewChildLogger());
            }
            else
            {
                return RegionalRepository.GetEnvironmentsArchiveJobActiveCountAsync(logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> GetEnvironmentsReadyForArchiveAsync(string idShard, int count, DateTime shutdownCutoffTime, DateTime softDeleteCutoffTime, IDiagnosticsLogger logger)
        {
            if (rolloutStatus == RolloutStatus.Phase1)
            {
                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                var globalEnvironments = await GlobalRepository.GetEnvironmentsReadyForArchiveAsync(idShard, count, shutdownCutoffTime, softDeleteCutoffTime, logger.NewChildLogger());
                var regionalEnvironments = await RegionalRepository.GetEnvironmentsReadyForArchiveAsync(idShard, count, shutdownCutoffTime, softDeleteCutoffTime, logger.NewChildLogger());

                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await RegionalRepository.GetEnvironmentsReadyForArchiveAsync(idShard, count, shutdownCutoffTime, softDeleteCutoffTime, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> GetEnvironmentsReadyForHardDeleteAsync(string idShard, DateTime cutoffTime, IDiagnosticsLogger logger)
        {
            if (rolloutStatus == RolloutStatus.Phase1)
            {
                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                var globalEnvironments = await GlobalRepository.GetEnvironmentsReadyForHardDeleteAsync(idShard, cutoffTime,  logger.NewChildLogger());
                var regionalEnvironments = await RegionalRepository.GetEnvironmentsReadyForHardDeleteAsync(idShard, cutoffTime, logger.NewChildLogger());

                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await RegionalRepository.GetEnvironmentsReadyForHardDeleteAsync(idShard, cutoffTime, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> GetFailedOperationAsync(string idShard, int count, IDiagnosticsLogger logger)
        {
            if (rolloutStatus == RolloutStatus.Phase1)
            {
                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                var globalEnvironments = await GlobalRepository.GetFailedOperationAsync(idShard, count, logger.NewChildLogger());
                var regionalEnvironments = await RegionalRepository.GetFailedOperationAsync(idShard, count, logger.NewChildLogger());

                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await RegionalRepository.GetFailedOperationAsync(idShard, count, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task ForEachEnvironmentWithComputeOrStorageAsync(AzureLocation controlPlaneLocation, string shardId, IDiagnosticsLogger logger, Func<CloudEnvironment, IDiagnosticsLogger, Task> itemCallback, Func<IEnumerable<CloudEnvironment>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            Expression<Func<CloudEnvironment, bool>> where = (x) => x.Id.StartsWith(shardId) && (x.Storage != null || x.Compute != null);
            var ids = new HashSet<string>();

            await RegionalRepository.ForEachAsync(
                where,
                logger.NewChildLogger(),
                (environment, childLogger) =>
                {
                    ids.Add(environment.Id);

                    return itemCallback(environment, childLogger);
                },
                pageResultsCallback);

            if (rolloutStatus == RolloutStatus.Phase1)
            {
                // Note: This will only be necessary until the environments in the global repository have been migrated to the appropriate regional repositories.
                where = (x) => x.Id.StartsWith(shardId) && x.ControlPlaneLocation == controlPlaneLocation && (x.Storage != null || x.Compute != null);
                await GlobalRepository.ForEachAsync(
                    where,
                    logger.NewChildLogger(),
                    (environment, childLogger) =>
                    {
                        if (ids.Contains(environment.Id))
                        {
                            return Task.CompletedTask;
                        }

                        return itemCallback(environment, childLogger);
                    },
                    pageResultsCallback);
            }
        }

        private IEnumerable<CloudEnvironment> MergeCloudEnvironments(IEnumerable<CloudEnvironment> globalEnvironments, IEnumerable<CloudEnvironment> regionalEnvironments)
        {
            var environments = new Dictionary<string, CloudEnvironment>();

            // Regional environment records take precedence over the global environment records.
            foreach (var environment in regionalEnvironments)
            {
                environments.Add(environment.Id, environment);
            }

            foreach (var environment in globalEnvironments)
            {
                if (!environments.ContainsKey(environment.Id))
                {
                    environments.Add(environment.Id, environment);
                }
            }

            return environments.Values;
        }

        private CloudEnvironment CreateGlobalEnvironment(CloudEnvironment environment)
        {
            // Note: Whatever properties we decide to include should be properties that cannot change.
            return new CloudEnvironment
            {
                Id = environment.Id,
                Type = environment.Type,
                PlanId = environment.PlanId,
                Created = environment.Created,
                OwnerId = environment.OwnerId,
                SkuName = environment.SkuName,
                Location = environment.Location,
                FriendlyName = environment.FriendlyName,
                ControlPlaneLocation = environment.ControlPlaneLocation,
            };
        }

        private void CopyCloudEnvironment(CloudEnvironment environment, CloudEnvironment target)
        {
            foreach (var property in environment.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                MethodInfo getter, setter;

                if ((getter = property.GetGetMethod(false)) == null ||
                    (setter = property.GetSetMethod(false)) == null)
                {
                    continue;
                }

                var value = getter.Invoke(environment, new object[0]);

                setter.Invoke(target, new object[1] { value });
            }
        }
    }
}
