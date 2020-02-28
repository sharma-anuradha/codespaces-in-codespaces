// <copyright file="IRelayStreamProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Interface contract to use on the relay hub stream class.
    /// </summary>
    public interface IRelayStreamProvider
    {
        /// <summary>
        /// Read async the next bytes available.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Next data available.</returns>
        Task<byte[]> ReadDataAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Encode the bytes to send.
        /// </summary>
        /// <param name="data">Raw data to encode.</param>
        /// <returns>The encoded data.</returns>
        byte[] EncodeHubData(byte[] data);

        /// <summary>
        /// Handle the next data received from the relay hub proxy.
        /// </summary>
        /// <param name="data">Raw data received.</param>
        void HandleReceivedData(byte[] data);
    }
}
