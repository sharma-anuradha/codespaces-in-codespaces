// <copyright file="MockRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Mocks
{
    /// <summary>
    /// Mock implementation for local development.
    /// </summary>
    public abstract class MockRepository<T> : IDocumentDbCollection<T>
        where T : IEntity, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockRepository{T}"/> class.
        /// </summary>
        public MockRepository()
        {
        }

        private ConcurrentDictionary<string, T> Store { get; } = new ConcurrentDictionary<string, T>();

        private Random Random { get; } = new Random();

        /// <inheritdoc/>
        public Task<IEnumerable<T>> GetWhereAsync(Expression<Func<T, bool>> where, IDiagnosticsLogger logger, Func<IEnumerable<T>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<TR>> QueryAsync<TR>(Func<IOrderedQueryable<T>, IQueryable<TR>> queryBuilder, IDiagnosticsLogger logger, Func<IEnumerable<TR>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<TR>> QueryAsync<TR>(Func<IDocumentClient, Uri, FeedOptions, IDocumentQuery<TR>> queryBuilder, IDiagnosticsLogger logger, Func<IEnumerable<TR>, IDiagnosticsLogger, Task> pageResultsCallback = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<T> UpdateAsync(T document, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));
            return Update(document);
        }

        /// <inheritdoc/>
        public async Task<T> CreateAsync(T document, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));
            return Create(document);
        }

        /// <inheritdoc/>
        public async Task<T> CreateOrUpdateAsync(T document, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));
            return Update(document);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));
            return Delete(key);
        }

        /// <inheritdoc/>
        public async Task<T> GetAsync(DocumentDbKey key, IDiagnosticsLogger logger)
        {
            await Task.Delay(Random.Next(100, 1000));
            return Get(key);
        }

        private T Get(DocumentDbKey key)
        {
            Store.TryGetValue(key.Id, out var resource);
            return resource;
        }

        private bool Delete(DocumentDbKey key)
        {
            return Store.TryRemove(key.Id, out var resource);
        }

        private T Update(T document)
        {
            Delete(document.Id);
            Create(document);
            return document;
        }

        private T Create(T document)
        {
            Store.TryAdd(document.Id, document);
            return document;
        }
    }
}
