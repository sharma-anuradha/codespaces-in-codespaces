// <copyright file="IBackplaneConnectorProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    public interface IBackplaneConnectorProvider
    {
        event EventHandler Disconnected;

        bool IsConnected { get; }

        void AddTarget(string methodName, Delegate handler);

        Task<TResult> InvokeAsync<TResult>(string targetName, object[] arguments, CancellationToken cancellationToken);

        Task AttemptConnectAsync(CancellationToken cancellationToken);
    }
}
