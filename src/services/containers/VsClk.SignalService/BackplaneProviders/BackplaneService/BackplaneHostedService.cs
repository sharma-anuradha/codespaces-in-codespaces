// <copyright file="BackplaneHostedService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// Background hosted service to control the backplane service lifetime
    /// </summary>
    public class BackplaneHostedService<TBackplaneServicType> : BackgroundService
        where TBackplaneServicType : class, IHostedBackplaneService
    {
        public BackplaneHostedService(TBackplaneServicType backplaneService)
        {
            BackplaneService = Requires.NotNull(backplaneService, nameof(backplaneService));
        }

        private TBackplaneServicType BackplaneService { get; }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return BackplaneService.DisposeAsync();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return BackplaneService.RunAsync(stoppingToken);
        }
    }
}
