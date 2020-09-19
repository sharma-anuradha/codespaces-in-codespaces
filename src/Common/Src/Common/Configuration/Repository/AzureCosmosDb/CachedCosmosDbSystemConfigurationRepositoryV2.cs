// <copyright file="CachedCosmosDbSystemConfigurationRepositoryV2.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.AzureCosmosDb
{
    /// <summary>
    /// A document db collection of <see cref="SystemConfigurationRecord"/>.
    /// </summary>
    [DocumentDbCollectionId(CollectionName)]
    public class CachedCosmosDbSystemConfigurationRepositoryV2 : DocumentDbCollection<SystemConfigurationRecord>, ICachedSystemConfigurationRepository
    {
        /// <summary>
        /// The cosmos db collection name.
        /// This is linked to the job schedule setting for CacheSystemConfigurationTask
        /// defined in the ResourceRegisterJobs and EnvironmentRegisterJobs
        /// </summary>
        public const string CollectionName = "configuration";

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedCosmosDbSystemConfigurationRepositoryV2"/> class.
        /// </summary>
        /// <param name="collectionOptions">The colleciton options.</param>
        /// <param name="clientProvider">The document db client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public CachedCosmosDbSystemConfigurationRepositoryV2(
            IOptionsMonitor<DocumentDbCollectionOptions> collectionOptions,
            IDocumentDbClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(
                  collectionOptions,
                  clientProvider,
                  healthProvider,
                  loggerFactory,
                  defaultLogValues)
        {
            Cache = new ConcurrentDictionary<string, CacheItem>();
        }

        /// <summary>
        /// Gets the cache object.
        /// </summary>
        private ConcurrentDictionary<string, CacheItem> Cache { get; }

        /// <summary>
        /// Configures the standard options for this repository.
        /// </summary>
        /// <param name="options">The options instance.</param>
        public static void ConfigureOptions(DocumentDbCollectionOptions options)
        {
            Requires.NotNull(options, nameof(options));
            options.PartitioningStrategy = PartitioningStrategy.IdOnly;
        }

        /// <inheritdoc/>
        public override Task<SystemConfigurationRecord> GetAsync(DocumentDbKey key, [ValidatedNotNull] IDiagnosticsLogger logger)
        {
            SystemConfigurationRecord document = default;

            return logger.OperationScopeAsync(
               $"docdb_{LoggingDocumentName}_get_call",
               async (childLogger) =>
               {
                   if (Cache.TryGetValue(key.Id, out var cacheItem))
                   {
                       AddDocumentId(key.Id, childLogger)
                           .LogInfo($"docdb_{LoggingDocumentName}_cache_found");
                       document = cacheItem.GetValue();
                   }
                   else
                   {
                       AddDocumentId(key.Id, childLogger)
                           .LogInfo($"docdb_{LoggingDocumentName}_cache_not_found");
                   }

                   return await Task.FromResult(document);
               }, swallowException: true);
        }

        /// <inheritdoc/>
        public async Task RefreshCacheAsync(IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
               $"docdb_{LoggingDocumentName}_refresh_cache",
               async (childLogger) =>
               {
                   var query = new SqlQuerySpec(@"SELECT * FROM c");
                   var records = await QueryAsync((client, uri, feedOptions) => client.CreateDocumentQuery<SystemConfigurationRecord>(uri, query, feedOptions).AsDocumentQuery(), childLogger.NewChildLogger());
                   childLogger.FluentAddBaseValue("SizeOfConfigurationRecords", records.Count());

                   // reusing the same childlogger to log various metrics and also because this is the only place from where this private method is beign called.
                   await UpdateCacheAsync(records, childLogger);
               }, swallowException: true);   
        }

        private Task UpdateCacheAsync(IEnumerable<SystemConfigurationRecord> records, IDiagnosticsLogger logger)
        {
            var oldkeys = new HashSet<string>(Cache.Keys);
            logger.FluentAddValue("TotalOldKeysCount", oldkeys.Count);

            var keyHitCounts = new Dictionary<string, uint>();
            foreach (var record in records)
            {
                var key = record.Id;
                var item = new CacheItem(record);
                Cache.AddOrUpdate(key, item, (k, oldItem) =>
                {
                    // value of the record in DB might have changed so return the new item but reuse the old hitcount
                    item.SetHitCount(oldItem.GetHitCount());
                    return item;
                });

                keyHitCounts.Add(key, item.GetHitCount());
                oldkeys.Remove(key);
            }

            logger.FluentAddValue("HitCounts", JsonConvert.SerializeObject(keyHitCounts));
            logger.FluentAddValue("DeletedKeysCount", oldkeys.Count);

            foreach (var key in oldkeys)
            {
                Cache.TryRemove(key, out var _);
            }

            return Task.CompletedTask;
        }
    }
}
