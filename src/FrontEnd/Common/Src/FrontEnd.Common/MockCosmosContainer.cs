// <copyright file="MockCosmosContainer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.VsSaas.Azure.Cosmos;
using Microsoft.VsSaaS.Azure.Cosmos;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Diagnostics;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Mock cosmos db container for testing purpose.
    /// </summary>
    /// <typeparam name="T">IEntity type. </typeparam>
    public class MockCosmosContainer<T> : ICosmosContainer<T>, IReadOnlyDictionary<string, T>
        where T : class, IEntity, new()
    {
        private readonly ConcurrentDictionary<string, T> store = new ConcurrentDictionary<string, T>();
        private readonly JsonSerializer jsonSerializer = new JsonSerializer();

        /// <inheritdoc/>
        public IEnumerable<string> Keys => store.Keys;

        /// <inheritdoc/>
        public IEnumerable<T> Values => store.Values;

        /// <inheritdoc/>
        public int Count => store.Count;

        /// <inheritdoc/>
        public T this[string key] => store[key];

        /// <inheritdoc/>
        public Task<T> CreateAsync(T item, IDiagnosticsLogger logger, ItemRequestOptions itemRequestOptions = null)
        {
            Requires.NotNullAllowStructs(item, nameof(item));
            Requires.NotNull(item.Id, nameof(item.Id));
            TestSerialization(item);

            if (!store.TryAdd(item.Id, item))
            {
                throw new InvalidOperationException("Mock: Cannot add entity: " + item.Id);
            }

            return Task.FromResult<T>(item);
        }

        /// <inheritdoc/>
        public Task<T> CreateOrUpdateAsync(T document, IDiagnosticsLogger logger, ItemRequestOptions options = null)
        {
            Requires.NotNullAllowStructs(document, nameof(document));
            Requires.NotNull(document.Id, nameof(document.Id));
            TestSerialization(document);

            return Task.FromResult<T>(store.AddOrUpdate(document.Id, document, (id, oldDocument) => document));
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(ItemKey key, IDiagnosticsLogger logger, ItemRequestOptions options = null)
        {
            return Task.FromResult(store.TryRemove(key.Id, out _));
        }

        /// <inheritdoc/>
        public Task<T> GetAsync(ItemKey key, IDiagnosticsLogger logger, ItemRequestOptions options = null, bool ignoreCache = false)
        {
            return Task.FromResult<T>(store[key.Id]);
        }

        /// <inheritdoc/>
        public Task<T> UpdateAsync(T item, IDiagnosticsLogger logger, ItemRequestOptions options = null)
        {
            Requires.NotNullAllowStructs(item, nameof(item));
            Requires.NotNull(item.Id, nameof(item.Id));
            TestSerialization(item);

            if (!store.TryGetValue(item.Id, out var existingDoc))
            {
                throw new InvalidOperationException("Mock: Entity to update not found: " + item.Id);
            }

            if (store.TryUpdate(item.Id, item, existingDoc))
            {
                return Task.FromResult(item);
            }
            else
            {
                return Task.FromResult(existingDoc);
            }
        }

        /// <inheritdoc/>
        public Task<QueryResults<T>> QueryAsync(
            string queryName,
            QueryDefinition queryDefinition,
            IDiagnosticsLogger logger,
            Func<FeedResponse<T>, IDiagnosticsLogger, Task> pagedCallback = null,
            Func<T, IDiagnosticsLogger, Task> itemCallback = null,
            QueryRequestOptions queryRequestOptions = null)
        {
            return QueryAsync<T>(queryName, queryDefinition, logger, pagedCallback, itemCallback);
        }

        /// <inheritdoc/>
        public async Task<QueryResults<TR>> QueryAsync<TR>(
            string queryName,
            QueryDefinition queryDefinition,
            IDiagnosticsLogger logger,
            Func<FeedResponse<TR>, IDiagnosticsLogger, Task> pagedCallback = null,
            Func<TR, IDiagnosticsLogger, Task> itemCallback = null,
            QueryRequestOptions queryRequestOptions = null)
        {
            await Task.CompletedTask;
            return new QueryResults<TR>(new ReadOnlyCollection<TR>(new TR[0]), 0.0);
        }

        /// <inheritdoc/>
        public async Task<QueryResults<TR>> QueryAsync<TR>(
            string queryName,
            IQueryable<TR> query,
            IDiagnosticsLogger logger,
            Func<FeedResponse<TR>,
            IDiagnosticsLogger, Task> pagedCallback = null,
            Func<TR, IDiagnosticsLogger, Task> itemCallback = null)
        {
            await Task.CompletedTask;
            var items = query.ToList().AsReadOnly();
            return new QueryResults<TR>(items, 0);
        }

        /// <inheritdoc/>
        public async Task<IQueryable<TR>> GetQueriableAsync<TR>(
            IDiagnosticsLogger logger,
            bool allowSynchronousQueryExecution = false,
            QueryRequestOptions queryRequestOptions = null)
        {
            await Task.CompletedTask;

            if (typeof(TR) != typeof(T))
            {
                return new List<TR>().AsQueryable();
            }

            return store.Values.AsQueryable<T>().Cast<TR>();
        }

        /// <inheritdoc/>
        public Task<CosmosClient> GetCosmosClientAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Microsoft.Azure.Cosmos.Database> GetDatabaseAsync()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Container> GetContainerAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// To clear.
        /// </summary>
        public void Clear()
        {
            store.Clear();
        }

        /// <inheritdoc/>
        public bool ContainsKey(string key) => store.ContainsKey(key);

        /// <inheritdoc/>
        public bool TryGetValue(string key, out T value) => store.TryGetValue(key, out value);

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, T>> GetEnumerator() => store.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)store).GetEnumerator();

        /// <summary>
        /// Serialize the document just to verify that it is serializable (all required properties are filled, etc.)
        /// </summary>
        private void TestSerialization(T document)
        {
            using (var stringWriter = new StringWriter())
            {
                jsonSerializer.Serialize(stringWriter, document);
            }
        }
    }
}
