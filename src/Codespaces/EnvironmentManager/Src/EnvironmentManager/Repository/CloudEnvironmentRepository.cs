// <copyright file="CloudEnvironmentRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Repository
{
    /// <summary>
    /// Manages the Global and Regional CloudEnvironment repositories.
    /// </summary>
    public class CloudEnvironmentRepository : ICloudEnvironmentRepository
    {
        private static readonly RolloutStatus DefaultRolloutStatus = RolloutStatus.Phase1;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentRepository"/> class.
        /// </summary>
        /// <param name="planRepository">The plan repository.</param>
        /// <param name="repositoryFactory">The regional cloud environment repository factory.</param>
        /// <param name="globalRepository">The global cloud environment repository.</param>
        /// <param name="regionalRepository">The regional cloud environment repository.</param>
        /// <param name="configurationReader">The configuration reader.</param>
        public CloudEnvironmentRepository(
            IPlanRepository planRepository,
            IRegionalCloudEnvironmentRepositoryFactory repositoryFactory,
            IGlobalCloudEnvironmentRepository globalRepository,
            IRegionalCloudEnvironmentRepository regionalRepository,
            IConfigurationReader configurationReader)
        {
            PlanRepository = Requires.NotNull(planRepository, nameof(planRepository));
            RepositoryFactory = Requires.NotNull(repositoryFactory, nameof(repositoryFactory));
            GlobalRepository = Requires.NotNull(globalRepository, nameof(globalRepository));
            RegionalRepository = Requires.NotNull(regionalRepository, nameof(regionalRepository));
            ConfigurationReader = configurationReader;
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

        private IRegionalCloudEnvironmentRepositoryFactory RepositoryFactory { get; }

        private IPlanRepository PlanRepository { get; }

        private IConfigurationReader ConfigurationReader { get; }

        private string LogBaseName => "cloud_environment_repository";

        /// <inheritdoc/>
        public async Task<CloudEnvironment> CreateAsync(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            if (environment.ControlPlaneLocation != RegionalRepository.ControlPlaneLocation)
            {
                throw new Exception($"Trying to create a CloudEnvironment record for {environment.ControlPlaneLocation} in {RegionalRepository.ControlPlaneLocation}.");
            }

            switch (await GetRolloutStatusAsync(logger))
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
                        environment.IsMigrated = true;

                        return await RegionalRepository.CreateAsync(environment, logger.NewChildLogger());
                    }
                    catch
                    {
                        environment.Id = null;
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
            switch (await GetRolloutStatusAsync(logger))
            {
                case RolloutStatus.Phase1:
                    // Note: We delete environments from the repositories in the same order that they were created in otherwise
                    // the migration worker task might restore the environment.
                    await GlobalRepository.DeleteAsync(id, logger.NewChildLogger());

                    return await logger.RetryOperationScopeAsync(
                        $"{LogBaseName}_delete_retry_scope",
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
            if (await GetRolloutStatusAsync(logger) == RolloutStatus.Phase1)
            {
                var environment = await RegionalRepository.GetAsync(id, logger.NewChildLogger());

                if (environment != null)
                {
                    environment.IsMigrated = true;
                    return environment;
                }

                // This returns the global repository's version of the CloudEnvironment if it doesn't (yet?) exist in the regional repository since it may have just not been migrated yet.
                environment = await GlobalRepository.GetAsync(id, logger.NewChildLogger());

                if (environment != null)
                {
                    // The environment isn't in the Regional db, so it isn't migrated.
                    environment.IsMigrated = false;
                }

                return environment;
            }
            else
            {
                return await RegionalRepository.GetAsync(id, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> ListAsync(string planId, AzureLocation? location, IDiagnosticsLogger logger)
        {
            Expression<Func<CloudEnvironment, bool>> where = (cloudEnvironment) => cloudEnvironment.PlanId == planId;

            if (await GetRolloutStatusAsync(logger) == RolloutStatus.Phase1)
            {
                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                var globalEnvironments = await GlobalRepository.GetWhereAsync(where, logger.NewChildLogger());
                var regionalEnvironments = await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());

                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await ListAsync(planId, location, where, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> ListAsync(string planId, AzureLocation? location, string friendlyNameInLowerCase, IDiagnosticsLogger logger)
        {
            Expression<Func<CloudEnvironment, bool>> where = (cloudEnvironment) => cloudEnvironment.PlanId == planId && cloudEnvironment.FriendlyNameInLowerCase == friendlyNameInLowerCase;

            if (await GetRolloutStatusAsync(logger) == RolloutStatus.Phase1)
            {
                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                var globalEnvironments = await GlobalRepository.GetWhereAsync(where, logger.NewChildLogger());
                var regionalEnvironments = await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());

                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await ListAsync(planId, location, where, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> ListAsync(UserIdSet userIdSet, IDiagnosticsLogger logger)
        {
            Expression<Func<CloudEnvironment, bool>> where = (cloudEnvironment) => (cloudEnvironment.OwnerId == userIdSet.CanonicalUserId ||
                cloudEnvironment.OwnerId == userIdSet.ProfileId);

            if (await GetRolloutStatusAsync(logger) == RolloutStatus.Phase1)
            {
                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                var globalEnvironments = await GlobalRepository.GetWhereAsync(where, logger.NewChildLogger());
                var regionalEnvironments = await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());

                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await ListAsync(userIdSet, where, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> ListAsync(UserIdSet userIdSet, string friendlyNameInLowerCase, IDiagnosticsLogger logger)
        {
            Expression<Func<CloudEnvironment, bool>> where = (cloudEnvironment) => (cloudEnvironment.OwnerId == userIdSet.CanonicalUserId ||
                cloudEnvironment.OwnerId == userIdSet.ProfileId) && cloudEnvironment.FriendlyNameInLowerCase == friendlyNameInLowerCase;

            if (await GetRolloutStatusAsync(logger) == RolloutStatus.Phase1)
            {
                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                var globalEnvironments = await GlobalRepository.GetWhereAsync(where, logger.NewChildLogger());
                var regionalEnvironments = await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());

                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await ListAsync(userIdSet, where, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> ListAsync(string planId, AzureLocation? location, UserIdSet userIdSet, IDiagnosticsLogger logger)
        {
            Expression<Func<CloudEnvironment, bool>> where = (cloudEnvironment) => (cloudEnvironment.OwnerId == userIdSet.CanonicalUserId ||
                cloudEnvironment.OwnerId == userIdSet.ProfileId) && cloudEnvironment.PlanId == planId;

            if (await GetRolloutStatusAsync(logger) == RolloutStatus.Phase1)
            {
                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                var globalEnvironments = await GlobalRepository.GetWhereAsync(where, logger.NewChildLogger());
                var regionalEnvironments = await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());

                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await ListAsync(planId, location, where, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> ListAsync(string planId, AzureLocation? location, UserIdSet userIdSet, string friendlyNameInLowerCase, IDiagnosticsLogger logger)
        {
            Expression<Func<CloudEnvironment, bool>> where = (cloudEnvironment) => (cloudEnvironment.OwnerId == userIdSet.CanonicalUserId ||
                cloudEnvironment.OwnerId == userIdSet.ProfileId) && cloudEnvironment.PlanId == planId &&
                cloudEnvironment.FriendlyNameInLowerCase == friendlyNameInLowerCase;

            if (await GetRolloutStatusAsync(logger) == RolloutStatus.Phase1)
            {
                // Note: We merge the global repository results in case the regional repository hasn't been (fully) populated by the migration yet.
                var globalEnvironments = await GlobalRepository.GetWhereAsync(where, logger.NewChildLogger());
                var regionalEnvironments = await RegionalRepository.GetWhereAsync(where, logger.NewChildLogger());

                return MergeCloudEnvironments(globalEnvironments, regionalEnvironments);
            }
            else
            {
                return await ListAsync(planId, location, where, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironment> UpdateAsync(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            if (environment.ControlPlaneLocation != RegionalRepository.ControlPlaneLocation)
            {
                throw new Exception($"Trying to update a CloudEnvironment record for {environment.ControlPlaneLocation} in {RegionalRepository.ControlPlaneLocation}.");
            }

            if (await GetRolloutStatusAsync(logger) == RolloutStatus.Phase1)
            {
                if (environment.IsMigrated)
                {
                    // The environment is known to exist in the regional database so we can just update it. If this fails, then it's no different from
                    // the way things used to work.
                    environment = await RegionalRepository.UpdateAsync(environment, logger.NewChildLogger());
                }
                else
                {
                    // The environment hadn't (yet?) been migrated when it was fetched. Try creating it in the regional database. If that fails,
                    // then hopefully our caller will properly handle the situation.
                    environment.IsMigrated = true;

                    environment = await RegionalRepository.CreateAsync(environment, logger.NewChildLogger());
                }

                // Update the record in the global repository as well so that the Get*Count() methods continue to work.
                var globalEnvironment = await logger.RetryOperationScopeAsync(
                   $"{LogBaseName}_update_getglobal_retry_scope",
                   async (retryLogger) =>
                   {
                       return await GlobalRepository.GetAsync(environment.Id, logger.NewChildLogger());
                   });

                if (globalEnvironment != null)
                {
                    CopyCloudEnvironment(environment, globalEnvironment);

                    await logger.RetryOperationScopeAsync(
                       $"{LogBaseName}_update_global_retry_scope",
                       async (retryLogger) =>
                       {
                           return await GlobalRepository.UpdateAsync(globalEnvironment, retryLogger);
                       });
                }

                return environment;
            }
            else
            {
                environment.IsMigrated = true;

                return await RegionalRepository.UpdateAsync(environment, logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> GetAllEnvironmentsInSubscriptionAsync(string subscriptionId, IDiagnosticsLogger logger)
        {
            if (await GetRolloutStatusAsync(logger) == RolloutStatus.Phase1)
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
        public async Task<int> GetCloudEnvironmentPlanCountAsync(IDiagnosticsLogger logger)
        {
            if (await GetRolloutStatusAsync(logger) == RolloutStatus.Phase1)
            {
                return await GlobalRepository.GetCloudEnvironmentPlanCountAsync(logger.NewChildLogger());
            }
            else
            {
                return await RegionalRepository.GetCloudEnvironmentPlanCountAsync(logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetCloudEnvironmentSubscriptionCountAsync(IDiagnosticsLogger logger)
        {
            if (await GetRolloutStatusAsync(logger) == RolloutStatus.Phase1)
            {
                return await GlobalRepository.GetCloudEnvironmentSubscriptionCountAsync(logger.NewChildLogger());
            }
            else
            {
                return await RegionalRepository.GetCloudEnvironmentSubscriptionCountAsync(logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetEnvironmentsArchiveJobActiveCountAsync(IDiagnosticsLogger logger)
        {
            if (await GetRolloutStatusAsync(logger) == RolloutStatus.Phase1)
            {
                return await GlobalRepository.GetEnvironmentsArchiveJobActiveCountAsync(logger.NewChildLogger());
            }
            else
            {
                return await RegionalRepository.GetEnvironmentsArchiveJobActiveCountAsync(logger.NewChildLogger());
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> GetEnvironmentsReadyForArchiveAsync(string idShard, int count, DateTime shutdownCutoffTime, DateTime softDeleteCutoffTime, IDiagnosticsLogger logger)
        {
            if (await GetRolloutStatusAsync(logger) == RolloutStatus.Phase1)
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
            if (await GetRolloutStatusAsync(logger) == RolloutStatus.Phase1)
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
            if (await GetRolloutStatusAsync(logger) == RolloutStatus.Phase1)
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
            var rolloutStatus = await GetRolloutStatusAsync(logger);
            var ids = new HashSet<string>();

            await RegionalRepository.ForEachAsync(
                where,
                logger.NewChildLogger(),
                (environment, childLogger) =>
                {
                    if (rolloutStatus == RolloutStatus.Phase1)
                    {
                        ids.Add(environment.Id);
                    }

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

        private async Task<RolloutStatus> GetRolloutStatusAsync(IDiagnosticsLogger logger)
        {
            // Note: a null ConfigurationReader is only for the unit tests
            if (ConfigurationReader == null)
            {
                return DefaultRolloutStatus;
            }

            return await logger.OperationScopeAsync(
                $"{LogBaseName}_get_rollout_status",
                async (childLogger) =>
                {
                    return await ConfigurationReader.ReadSettingAsync(nameof(CloudEnvironmentRepository), "rollout-status", childLogger.NewChildLogger(), DefaultRolloutStatus);
                },
                swallowException: true);
        }

        private async Task<VsoPlan> GetPlanAsync(string planId, IDiagnosticsLogger logger)
        {
            if (!VsoPlanInfo.TryParse(planId, out var planInfo))
            {
                return null;
            }

            var plans = await PlanRepository.GetWhereAsync(
                (model) => model.Plan.Subscription == planInfo.Subscription &&
                           model.Plan.ResourceGroup == planInfo.ResourceGroup &&
                           model.Plan.Name == planInfo.Name,
                logger.NewChildLogger(),
                null);

            return plans.FirstOrDefault(x => !x.IsDeleted);
        }

        private Task<IEnumerable<VsoPlan>> GetPlansAsync(UserIdSet userIdSet, IDiagnosticsLogger logger)
        {
            return PlanRepository.GetWhereAsync(x => x.UserId == userIdSet.CanonicalUserId || x.UserId == userIdSet.ProfileId, logger.NewChildLogger());
        }

        private IRegionalCloudEnvironmentRepository GetCloudEnvironmentRepository(AzureLocation controlPlaneLocation, IDiagnosticsLogger logger)
        {
            if (controlPlaneLocation == RegionalRepository.ControlPlaneLocation)
            {
                return RegionalRepository;
            }

            return RepositoryFactory.GetRegionalRepository(controlPlaneLocation, logger.NewChildLogger());
        }

        private async Task<IEnumerable<CloudEnvironment>> ListAsync(string planId, AzureLocation? location, Expression<Func<CloudEnvironment, bool>> where, IDiagnosticsLogger logger)
        {
            if (!location.HasValue)
            {
                location = await logger.OperationScopeAsync(
                    $"{LogBaseName}_get_plan_location",
                    async (childLogger) =>
                    {
                        var plan = await GetPlanAsync(planId, childLogger);

                        return plan?.Plan.Location;
                    },
                    swallowException: true);

                if (!location.HasValue)
                {
                    return Enumerable.Empty<CloudEnvironment>();
                }
            }

            var repository = GetCloudEnvironmentRepository(location.Value, logger);

            return await repository.GetWhereAsync(where, logger.NewChildLogger());
        }

        private async Task<IEnumerable<CloudEnvironment>> ListAsync(UserIdSet userIdSet, Expression<Func<CloudEnvironment, bool>> where, IDiagnosticsLogger logger)
        {
            var plans = await GetPlansAsync(userIdSet, logger);
            var environments = new List<CloudEnvironment>();

            foreach (var plan in plans.Where(x => !x.IsDeleted))
            {
                var repository = GetCloudEnvironmentRepository(plan.Plan.Location, logger);

                await logger.OperationScopeAsync(
                    $"{LogBaseName}_list_by_userid",
                    async (childLogger) =>
                    {
                        childLogger.AddBaseValue("RegionalLocation", plan.Plan.Location.ToString());
                        environments.AddRange(await repository.GetWhereAsync(where, childLogger.NewChildLogger()));
                    },
                    swallowException: true);
            }

            return environments;
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
                IsMigrated = true,
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
