// <copyright file="IHostedBackplaneService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// A hosted backplane service contract
    /// </summary>
    public interface IHostedBackplaneService
    {
        Task RunAsync(CancellationToken stoppingToken);

        Task DisposeAsync();
    }
}
