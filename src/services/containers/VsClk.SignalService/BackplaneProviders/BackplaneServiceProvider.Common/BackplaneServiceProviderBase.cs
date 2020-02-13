// <copyright file="BackplaneServiceProviderBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VsCloudKernel.SignalService
{
    public abstract class BackplaneServiceProviderBase
    {
        private const int TimeoutConnectionMillisecs = 2000;
        private const string RegisterServiceMethod = "RegisterService";

        private TaskCompletionSource<bool> connectedTcs;

        protected BackplaneServiceProviderBase(
            IBackplaneConnectorProvider backplaneConnectorProvider,
            string hostServiceId,
            CancellationToken stoppingToken)
        {
            BackplaneConnectorProvider = backplaneConnectorProvider;
            HostServiceId = hostServiceId;

            backplaneConnectorProvider.Disconnected += (s, e) =>
            {
                Task.Run(() => AttemptConnectAsync(stoppingToken)).Forget();
            };
        }

        protected IBackplaneConnectorProvider BackplaneConnectorProvider { get; }

        protected abstract string ServiceType { get; }

        protected string HostServiceId { get; }

        private Task ConnectedTask => this.connectedTcs.Task;

        private bool IsConnected => BackplaneConnectorProvider.IsConnected;

        public async Task AttemptConnectAsync(CancellationToken cancellationToken)
        {
            this.connectedTcs = new TaskCompletionSource<bool>();
            await BackplaneConnectorProvider.AttemptConnectAsync(cancellationToken);
            await BackplaneConnectorProvider.InvokeAsync<object>(RegisterServiceMethod, new object[] { ServiceType, HostServiceId }, cancellationToken);

            this.connectedTcs.TrySetResult(true);
        }

        protected async Task EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            if (!IsConnected)
            {
                if (await Task.WhenAny(ConnectedTask, Task.Delay(TimeoutConnectionMillisecs, cancellationToken)) != ConnectedTask)
                {
                    throw new TimeoutException("Waiting to connect on backplane server");
                }
            }
        }
    }
}
