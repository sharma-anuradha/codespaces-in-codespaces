// <copyright file="IRelayServiceProxy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// The client relay service proxy.
    /// </summary>
    public interface IRelayServiceProxy : IServiceProxyBase
    {
        /// <summary>
        /// Create a hub.
        /// </summary>
        /// <param name="hubId">The hub unique identifier.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The hub created identifier.</returns>
        Task<string> CreateHubAsync(string hubId, CancellationToken cancellationToken);

        /// <summary>
        /// Delete a hub that was previously created.
        /// </summary>
        /// <param name="hubId">The hub unique identifier.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task completion.</returns>
        Task DeleteHubAsync(string hubId, CancellationToken cancellationToken);

        /// <summary>
        /// Join an existing hub.
        /// </summary>
        /// <param name="hubId">The hub unique identifier.</param>
        /// <param name="properties">Which properties to publish.</param>
        /// <param name="createIfNotExists">Create the hub if it does not exists.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A relay hub proxy.</returns>
        Task<IRelayHubProxy> JoinHubAsync(string hubId, Dictionary<string, object> properties, bool createIfNotExists, CancellationToken cancellationToken);
    }
}
