// <copyright file="MockRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Diagnostics;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Mock repository for testing purpose.
    /// </summary>
    /// <typeparam name="T">IEntity type. </typeparam>
    public class MockRepository<T> : IDocumentDbCollection<T>, IReadOnlyDictionary<string, T>
        where T : IEntity
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
        public Task<T> CreateAsync(T document, IDiagnosticsLogger logger)
        {
            Requires.NotNullAllowStructs(document, nameof(document));
            Requires.NotNull(document.Id, nameof(document.Id));
            TestSerialization(document);

            if (!store.TryAdd(document.Id, document))
            {
                throw new InvalidOperationException("Mock: Cannot add entity: " + document.Id);
            }

            return Task.FromResult<T>(document);
        }

        /// <inheritdoc/>
        public Task<T> CreateOrUpdateAsync(T document, IDiagnosticsLogger logger)
        {
            Requires.NotNullAllowStructs(document, nameof(document));
            Requires.NotNull(document.Id, nameof(document.Id));
            TestSerialization(document);

            return Task.FromResult<T>(store.AddOrUpdate(document.Id, document, (id, oldDocument) => document));
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            return Task.FromResult(store.TryRemove(key.Id, out _));
        }

        /// <inheritdoc/>
        public Task<T> GetAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            return Task.FromResult<T>(store[key.Id]);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<T>> GetWhereAsync(Expression<Func<T, bool>> where, IDiagnosticsLogger logger, Func<IEnumerable<T>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            return Task.FromResult<IEnumerable<T>>(store.Values.Where(where.Compile()));
        }

        /// <inheritdoc/>
        public Task<IEnumerable<TR>> QueryAsync<TR>(Func<IOrderedQueryable<T>, IQueryable<TR>> queryBuilder, IDiagnosticsLogger logger, Func<IEnumerable<TR>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            return Task.FromResult<IEnumerable<TR>>(queryBuilder(store.Values.AsQueryable().OrderBy(s => 1)).AsEnumerable());
        }

        /// <inheritdoc/>
        public Task<IEnumerable<TR>> QueryAsync<TR>(Func<IDocumentClient, Uri, FeedOptions, IDocumentQuery<TR>> queryBuilder, IDiagnosticsLogger logger, Func<IEnumerable<TR>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<T> UpdateAsync(T document, IDiagnosticsLogger logger)
        {
            Requires.NotNullAllowStructs(document, nameof(document));
            Requires.NotNull(document.Id, nameof(document.Id));
            TestSerialization(document);

            if (!store.TryGetValue(document.Id, out var existingDoc))
            {
                throw new InvalidOperationException("Mock: Entity to update not found: " + document.Id);
            }

            if (store.TryUpdate(document.Id, document, existingDoc))
            {
                return Task.FromResult(document);
            }
            else
            {
                return Task.FromResult(existingDoc);
            }
        }

        /// <inheritdoc/>
        public Task ForEachAsync(Expression<Func<T, bool>> where, IDiagnosticsLogger logger, Func<T, IDiagnosticsLogger, Task> itemCallback, Func<IEnumerable<T>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ForEachAsync<TR>(Func<IOrderedQueryable<T>, IQueryable<TR>> queryBuilder, IDiagnosticsLogger logger, Func<TR, IDiagnosticsLogger, Task> itemCallback, Func<IEnumerable<TR>, IDiagnosticsLogger, Task> pageResultsCallback = null)
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
