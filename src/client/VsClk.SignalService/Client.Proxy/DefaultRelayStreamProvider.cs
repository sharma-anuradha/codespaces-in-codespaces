// <copyright file="DefaultRelayStreamProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Our default data relay stream provider.
    /// </summary>
    public class DefaultRelayStreamProvider : IRelayStreamProvider
    {
        private readonly BufferBlock<byte[]> buffer = new BufferBlock<byte[]>();

        /// <inheritdoc/>
        public Task<byte[]> ReadDataAsync(CancellationToken cancellationToken)
        {
            return this.buffer.ReceiveAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public byte[] EncodeHubData(byte[] data)
        {
            return data;
        }

        /// <inheritdoc/>
        public void HandleReceivedData(byte[] data)
        {
            this.buffer.Post(data);
        }
    }
}
