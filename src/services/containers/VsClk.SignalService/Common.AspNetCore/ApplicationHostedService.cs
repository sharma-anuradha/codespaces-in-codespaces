// <copyright file="ApplicationHostedService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Background hosted service to control the backplane service lifetime.
    /// </summary>
    /// <typeparam name="TBackplaneServicType">Type of the hosted service.</typeparam>
    public class ApplicationHostedService<TBackplaneServicType> : BackgroundService
        where TBackplaneServicType : class, IHostedService
    {
        private readonly ILogger logger;

        public ApplicationHostedService(TBackplaneServicType hostedService, ILogger<ApplicationHostedService<TBackplaneServicType>> logger)
        {
            HostedService = Requires.NotNull(hostedService, nameof(hostedService));
            this.logger = logger;
        }

        private TBackplaneServicType HostedService { get; }

        /// <inheritdoc/>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return HostedService.DisposeAsync();
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                await this.logger.InvokeWithUnhandledErrorAsync(() => HostedService.RunAsync(stoppingToken));
            }
        }
    }
}
