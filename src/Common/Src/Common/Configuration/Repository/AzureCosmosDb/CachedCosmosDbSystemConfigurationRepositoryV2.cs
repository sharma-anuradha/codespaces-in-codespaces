// <copyright file="CachedCosmosDbSystemConfigurationRepositoryV2.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
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
        /// A property to check if the cache had been hydrated at least once.
        /// If it is not the case, then the Get API should make use of base class
        /// while the cache is being hydrated via warmup task.
        /// </summary>
        private bool IsCacheHydrated { get; set; } = false;

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
            return logger.OperationScopeAsync(
               $"docdb_{LoggingDocumentName}_get_call",
               async (childLogger) =>
               {
                   SystemConfigurationRecord document = default;
                   childLogger.FluentAddValue("IsCacheHydrated", IsCacheHydrated);

                   if (IsCacheHydrated)
                   {
                       var isFound = Cache.TryGetValue(key.Id, out var cacheItem);
                       if (isFound)
                       {
                           document = cacheItem.GetValue();
                       }

                       childLogger.FluentAddValue("FoundInCache", isFound);
                   }
                   else
                   {
                       document = await base.GetAsync(key, childLogger);
                   }

                   childLogger.AddDocumentId(key.Id);
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

                   // set the hydration flag to true to make sure the following calls to GetAsync() API makes use of cache instead of making calls to cosmos DB
                   IsCacheHydrated = true;
               }, swallowException: true);
        }

        private async Task UpdateCacheAsync(IEnumerable<SystemConfigurationRecord> records, IDiagnosticsLogger logger)
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

            var subHitCountDicts = SplitHitCountDictionary(keyHitCounts);

            foreach (var dict in subHitCountDicts)
            {
                await logger.OperationScopeAsync(
                $"docdb_{LoggingDocumentName}_hit_count",
                async (hitCountLogger) =>
                {
                    hitCountLogger.FluentAddValue("HitCounts", JsonConvert.SerializeObject(dict));
                    await Task.FromResult(true);
                }, swallowException: true);
            }

            logger.FluentAddValue("DeletedKeysCount", oldkeys.Count);

            foreach (var key in oldkeys)
            {
                Cache.TryRemove(key, out var _);
            }
        }

        private IList<IDictionary<string, uint>> SplitHitCountDictionary(IDictionary<string, uint> keyHitCounts, int maxSizePerDictionary = 50)
        {
            var list = new List<IDictionary<string, uint>>();
            var listCount = keyHitCounts.Count() / maxSizePerDictionary;

            if (keyHitCounts.Count % maxSizePerDictionary > 0)
                listCount++;

            for (var i = 0; i < listCount; i++)
            {
                var dict = keyHitCounts.Skip(i * maxSizePerDictionary).Take(maxSizePerDictionary).ToDictionary(c => c.Key, c => c.Value);
                list.Add(dict);
            }

            return list;
        }
    }
}
