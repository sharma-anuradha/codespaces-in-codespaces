// <copyright file="DisposableHostedService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Background hosted service to control the backplane service lifetime.
    /// </summary>
    /// <typeparam name="TBackplaneServicType">Type of the hosted service.</typeparam>
    public class DisposableHostedService<TBackplaneServicType> : BackgroundService
        where TBackplaneServicType : class, IAsyncDisposable
    {
        public DisposableHostedService(TBackplaneServicType hostedService)
        {
            HostedService = Requires.NotNull(hostedService, nameof(hostedService));
        }

        private TBackplaneServicType HostedService { get; }

        /// <inheritdoc/>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await HostedService.DisposeAsync();
        }

        /// <inheritdoc/>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}
