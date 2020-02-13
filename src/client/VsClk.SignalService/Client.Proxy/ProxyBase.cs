// <copyright file="ProxyBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Proxy base class.
    /// </summary>
    public class ProxyBase : IServiceProxyBase, IDisposable
    {
        private readonly List<IDisposable> onHubHandlers = new List<IDisposable>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyBase"/> class.
        /// </summary>
        /// <param name="hubProxy">The hub proxy instance.</param>
        protected ProxyBase(IHubProxy hubProxy)
        {
            HubProxy = Requires.NotNull(hubProxy, nameof(hubProxy));
        }

        /// <inheritdoc/>
        public IHubProxy HubProxy { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var disposable in this.onHubHandlers)
            {
                disposable.Dispose();
            }

            this.onHubHandlers.Clear();
        }

        /// <summary>
        /// Add a hub handler.
        /// </summary>
        /// <param name="disposable">Disposable handler</param>
        protected void AddHubHandler(IDisposable disposable)
        {
            this.onHubHandlers.Add(disposable);
        }
    }
}
