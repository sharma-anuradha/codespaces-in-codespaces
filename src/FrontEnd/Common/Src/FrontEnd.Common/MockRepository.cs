// <copyright file="MockRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    public class MockRepository<T> : IDocumentDbCollection<T>, IReadOnlyDictionary<string, T>
        where T : IEntity
    {
        private readonly ConcurrentDictionary<string, T> store = new ConcurrentDictionary<string, T>();

        public IEnumerable<string> Keys => this.store.Keys;

        public IEnumerable<T> Values => this.store.Values;

        public int Count => this.store.Count;

        public T this[string key] => this.store[key];

        public Task<T> CreateAsync(T document, IDiagnosticsLogger logger)
        {
            Requires.NotNullAllowStructs(document, nameof(document));
            Requires.NotNull(document.Id, nameof(document.Id));

            if (!this.store.TryAdd(document.Id, document))
            {
                throw new InvalidOperationException("Mock: Cannot add entity: " + document.Id);
            }

            return Task.FromResult<T>(document);
        }

        public Task<T> CreateOrUpdateAsync(T document, IDiagnosticsLogger logger)
        {
            Requires.NotNullAllowStructs(document, nameof(document));
            Requires.NotNull(document.Id, nameof(document.Id));

            return Task.FromResult<T>(this.store.AddOrUpdate(document.Id, document, (id, oldDocument) => document));
        }

        public Task<bool> DeleteAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            return Task.FromResult(this.store.TryRemove(key.Id, out _));
        }

        public Task<T> GetAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            return Task.FromResult<T>(this.store[key.Id]);
        }

        public Task<IEnumerable<T>> GetWhereAsync(Expression<Func<T, bool>> where, IDiagnosticsLogger logger, Func<IEnumerable<T>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            return Task.FromResult<IEnumerable<T>>(this.store.Values.Where(where.Compile()));
        }

        public Task<IEnumerable<TR>> QueryAsync<TR>(Func<IOrderedQueryable<T>, IQueryable<TR>> queryBuilder, IDiagnosticsLogger logger, Func<IEnumerable<TR>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            return Task.FromResult<IEnumerable<TR>>(queryBuilder(this.store.Values.AsQueryable().OrderBy(s => 1)).AsEnumerable());
        }

        public Task<IEnumerable<TR>> QueryAsync<TR>(Func<IDocumentClient, Uri, FeedOptions, IDocumentQuery<TR>> queryBuilder, IDiagnosticsLogger logger, Func<IEnumerable<TR>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            throw new NotImplementedException();
        }

        public Task<T> UpdateAsync(T document, IDiagnosticsLogger logger)
        {
            Requires.NotNullAllowStructs(document, nameof(document));
            Requires.NotNull(document.Id, nameof(document.Id));

            if (!this.store.TryGetValue(document.Id, out var existingDoc))
            {
                throw new InvalidOperationException("Mock: Entity to update not found: " + document.Id);
            }

            if (this.store.TryUpdate(document.Id, document, existingDoc))
            {
                return Task.FromResult(document);
            }
            else
            {
                return Task.FromResult(existingDoc);
            }
        }

        public void Clear()
        {
            this.store.Clear();
        }

        public bool ContainsKey(string key) => this.store.ContainsKey(key);

        public bool TryGetValue(string key, out T value) => this.store.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator() => this.store.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.store).GetEnumerator();
    }
}
