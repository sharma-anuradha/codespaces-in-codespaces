// <copyright file="RelayDataHubProxy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// An implementation of the IRelayDataHubProxy interface using a filter callback.
    /// </summary>
    public class RelayDataHubProxy : IRelayDataHubProxy, IDisposable
    {
        private readonly IRelayDataHubProxy relayDataHubProxy;
        private readonly Func<ReceiveDataEventArgs, bool> filterEventCallback;

        public RelayDataHubProxy(
            IRelayDataHubProxy relayDataHubProxy,
            Func<ReceiveDataEventArgs, bool> filterEventCallback)
        {
            this.relayDataHubProxy = Requires.NotNull(relayDataHubProxy, nameof(relayDataHubProxy));
            this.filterEventCallback = Requires.NotNull(filterEventCallback, nameof(filterEventCallback));
            relayDataHubProxy.ReceiveData += OnReceiveData;
        }

        /// <inheritdoc/>
        public event EventHandler<ReceiveDataEventArgs> ReceiveData;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.relayDataHubProxy.ReceiveData -= OnReceiveData;
        }

        /// <summary>
        /// process received data.
        /// </summary>
        /// <param name="e">The data received event.</param>
        protected virtual void ProcessReceiveData(ReceiveDataEventArgs e)
        {
            FireReceiveData(e);
        }

        /// <summary>
        /// Fire a received data on our event sink.
        /// </summary>
        /// <param name="e">The data received event.</param>
        protected void FireReceiveData(ReceiveDataEventArgs e)
        {
            ReceiveData?.Invoke(this, e);
        }

        private void OnReceiveData(object sender, ReceiveDataEventArgs e)
        {
            if (!this.filterEventCallback(e))
            {
                return;
            }

            ProcessReceiveData(e);
        }
    }
}
