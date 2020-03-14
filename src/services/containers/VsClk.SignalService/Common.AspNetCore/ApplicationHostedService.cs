// <copyright file="ApplicationHostedService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Background hosted service to control the backplane service lifetime.
    /// </summary>
    /// <typeparam name="TBackplaneServicType">Type of the hosted service.</typeparam>
    public class ApplicationHostedService<TBackplaneServicType> : BackgroundService
        where TBackplaneServicType : class, IHostedService
    {
        public ApplicationHostedService(TBackplaneServicType hostedService)
        {
            HostedService = Requires.NotNull(hostedService, nameof(hostedService));
        }

        private TBackplaneServicType HostedService { get; }

        /// <inheritdoc/>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return HostedService.DisposeAsync();
        }

        /// <inheritdoc/>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return HostedService.RunAsync(stoppingToken);
        }
    }
}
