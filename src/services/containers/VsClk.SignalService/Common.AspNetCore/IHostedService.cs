// <copyright file="IHostedService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// A hosted service contract.
    /// </summary>
    public interface IHostedService
    {
        /// <summary>
        /// Run the service during the host lifetime.
        /// </summary>
        /// <param name="stoppingToken">A stopping token to pass.</param>
        /// <returns>Completion task.</returns>
        Task RunAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Dispose this service when the host is being shutdown.
        /// </summary>
        /// <returns>Completion task.</returns>
        Task DisposeAsync();
    }
}
