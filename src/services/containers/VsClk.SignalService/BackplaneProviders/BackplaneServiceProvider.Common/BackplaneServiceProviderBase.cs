// <copyright file="BackplaneServiceProviderBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.Services.Backplane.Common;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
    public abstract class BackplaneServiceProviderBase
    {
        private const int TimeoutConnectionMillisecs = 2000;
        private const int MaxAttemptsToWait = 5;
        private const string RegisterServiceMethod = "RegisterService";

        private TaskCompletionSource<bool> connectedTcs;
        private int numberOfAttempts;

        protected BackplaneServiceProviderBase(
            IBackplaneConnectorProvider backplaneConnectorProvider,
            string hostServiceId,
            ILogger logger,
            CancellationToken stoppingToken)
        {
            BackplaneConnectorProvider = Requires.NotNull(backplaneConnectorProvider, nameof(backplaneConnectorProvider));
            HostServiceId = hostServiceId;
            Logger = Requires.NotNull(logger, nameof(logger));

            backplaneConnectorProvider.Disconnected += (s, e) =>
            {
                this.numberOfAttempts = 0;
                Task.Run(() => AttemptConnectAsync(stoppingToken)).Forget();
            };
        }

        protected IBackplaneConnectorProvider BackplaneConnectorProvider { get; }

        protected abstract string ServiceType { get; }

        protected ILogger Logger { get; }

        protected string HostServiceId { get; }

        private Task ConnectedTask => this.connectedTcs.Task;

        private bool IsConnected => BackplaneConnectorProvider.IsConnected;

        public async Task AttemptConnectAsync(CancellationToken cancellationToken)
        {
            const int TimeoutRetryFailureMillisecs = 500;

            this.connectedTcs = new TaskCompletionSource<bool>();

            while (true)
            {
                if (await Logger.InvokeWithUnhandledErrorAsync(async () =>
                {
                    await BackplaneConnectorProvider.AttemptConnectAsync(cancellationToken);
                    await BackplaneConnectorProvider.InvokeAsync<object>(RegisterServiceMethod, new object[] { ServiceType, HostServiceId }, cancellationToken);
                }))
                {
                    break;
                }

                // await after the unexpected failure
                await Task.Delay(TimeoutRetryFailureMillisecs, cancellationToken);
            }

            this.connectedTcs.TrySetResult(true);
        }

        protected async Task EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            if (!IsConnected)
            {
                if (this.connectedTcs == null || this.numberOfAttempts > MaxAttemptsToWait)
                {
                    throw new BackplaneNotAvailableException();
                }

                if (await Task.WhenAny(ConnectedTask, Task.Delay(TimeoutConnectionMillisecs, cancellationToken)) != ConnectedTask)
                {
                    ++this.numberOfAttempts;
                    throw new TimeoutException("Waiting to connect on backplane server");
                }
            }
        }
    }
}
