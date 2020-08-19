// <copyright file="BufferBlockQueueFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Implements IQueueFactory interface based on a TPL buffer block.
    /// </summary>
    public class BufferBlockQueueFactory : DisposableBase, IQueueFactory
    {
        private readonly ConcurrentDictionary<(string, AzureLocation?), BufferBlockQueue> bufferBlockQueues = new ConcurrentDictionary<(string, AzureLocation?), BufferBlockQueue>();

        /// <inheritdoc/>
        public IQueue GetOrCreate(string queueId, AzureLocation? azureLocation)
        {
            Requires.NotNullOrEmpty(queueId, nameof(queueId));
            return this.bufferBlockQueues.GetOrAdd((queueId, azureLocation), (id) => new BufferBlockQueue(queueId));
        }

        /// <inheritdoc/>
        protected override async Task DisposeInternalAsync()
        {
            await Task.WhenAll(this.bufferBlockQueues.Values.Select(i => i.DisposeAsync().AsTask()));
        }
    }
}
