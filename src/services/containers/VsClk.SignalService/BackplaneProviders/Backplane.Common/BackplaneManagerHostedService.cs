// <copyright file="BackplaneManagerHostedService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.VsCloudKernel.SignalService;

namespace Microsoft.VsCloudKernel.Services.Backplane.Common
{
    /// <summary>
    /// Background long running service to wrap the IBackplaneManagerService instance
    /// </summary>
    public class BackplaneManagerHostedService<T, TBackplaneManager> : BackgroundService
        where TBackplaneManager : IBackplaneManagerBase
    {
        public BackplaneManagerHostedService(TBackplaneManager backplaneManager, T service)
        {
            // Note: we define the T as a service to be injected when this hosted service
            // is being constructed. The reason is that we want also the service to define
            // the 'MetricsFactory' property, otherwise the service could be created on demand.
            BackplaneManager = backplaneManager;
        }

        private TBackplaneManager BackplaneManager { get; }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return BackplaneManager.DisposeAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return BackplaneManager.RunAsync(stoppingToken);
        }
    }
}
