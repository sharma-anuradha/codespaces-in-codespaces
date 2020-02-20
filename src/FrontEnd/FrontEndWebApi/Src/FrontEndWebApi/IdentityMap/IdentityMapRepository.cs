// <copyright file="IdentityMapRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.IdentityMap
{
    /// <summary>
    /// A document repository of <see cref="IIdentityMapEntity"/>.
    /// </summary>
    [DocumentDbCollectionId(CloudEnvironmentsCollectionId)]
    public class IdentityMapRepository : DocumentDbCollectionCached<IdentityMapEntity>, IIdentityMapRepository
    {
        /// <summary>
        /// The models collection id.
        /// </summary>
        public const string CloudEnvironmentsCollectionId = "identity_map";

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityMapRepository"/> class.
        /// </summary>
        /// <param name="options">The collection options snapshot.</param>
        /// <param name="clientProvider">The client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The diagnostics logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        /// <param name="managedCache">The managed cached provider.</param>
        /// <param name="taskHelper">The background task helper.</param>
        public IdentityMapRepository(
                IOptionsMonitor<DocumentDbCollectionOptions> options,
                IDocumentDbClientProvider clientProvider,
                IHealthProvider healthProvider,
                IDiagnosticsLoggerFactory loggerFactory,
                LogValueSet defaultLogValues,
                IManagedCache managedCache,
                ITaskHelper taskHelper)
            : base(
                  options,
                  clientProvider,
                  healthProvider,
                  loggerFactory,
                  defaultLogValues,
                  managedCache)
        {
            TaskHelper = Requires.NotNull(taskHelper, nameof(taskHelper));
        }

        private ITaskHelper TaskHelper { get; }

        /// <summary>
        /// Configures the standard options for this repository.
        /// </summary>
        /// <param name="options">The options instance.</param>
        public static void ConfigureOptions(DocumentDbCollectionOptions options)
        {
            Requires.NotNull(options, nameof(options));
            options.PartitioningStrategy = PartitioningStrategy.IdOnly;
            options.CacheExpiry = TimeSpan.FromSeconds(30);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteByUserNameAsync(string userName, string tenantId, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(userName, nameof(userName));
            Requires.NotNullOrEmpty(tenantId, nameof(tenantId));
            Requires.NotNull(logger, nameof(logger));
            var id = IdentityMapEntity.MakeId(userName, tenantId);
            return await DeleteAsync(id, logger);
        }

        /// <inheritdoc/>
        public async Task<IIdentityMapEntity> GetByUserNameAsync(string userName, string tenantId, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(userName, nameof(userName));
            Requires.NotNullOrEmpty(tenantId, nameof(tenantId));
            Requires.NotNull(logger, nameof(logger));
            var id = IdentityMapEntity.MakeId(userName, tenantId);

            IIdentityMapEntity map = default;
            await Retry.DoAsync(async attempt =>
            {
                map = await GetAsync(id, logger);
            });

            return map;
        }

        /// <inheritdoc/>
        public async Task<IIdentityMapEntity> BackgroundUpdateIfChangedAsync(
            IIdentityMapEntity map,
            string canonicalUserId,
            string profileId,
            string profileProviderId,
            IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            Requires.NotNull(map, nameof(map));
            Requires.NotNull(logger, nameof(logger));

            bool update = false;

            if (canonicalUserId != null && map.CanonicalUserId != canonicalUserId)
            {
                map.CanonicalUserId = canonicalUserId;
                update = true;
            }

            if (profileId != null && map.ProfileId != profileId)
            {
                map.ProfileId = profileId;
                update = true;
            }

            if (profileProviderId != null && map.ProfileProviderId != profileProviderId)
            {
                map.ProfileProviderId = profileProviderId;
                update = true;
            }

            if (update)
            {
                // This is intentionally fire-and-forget.
                RunBackgroundCreateOrUpdate(map, logger);
            }

            return map;
        }

        // Because updates can happen during token authentication, we won't wait
        // for the udpate.
        private void RunBackgroundCreateOrUpdate(IIdentityMapEntity map, IDiagnosticsLogger logger)
        {
            // Pass in a copy to be updated, just ensure it is not externally mutated.
            var innerMap = new IdentityMapEntity(map.UserName, map.TenantId)
            {
                CanonicalUserId = map.CanonicalUserId,
                ProfileId = map.ProfileId,
                ProfileProviderId = map.ProfileProviderId,
            };

            TaskHelper.RunBackground(
                "identity_map_repository_update",
                innerLooger => CreateOrUpdateAsync(innerMap, innerLooger),
                logger);
        }
    }
}
