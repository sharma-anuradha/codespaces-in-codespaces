// <copyright file="ProxyBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;

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
        /// <param name="trace">Trace instance.</param>
        /// <param name="formatProvider">Optional format provider.</param>
        protected ProxyBase(IHubProxy hubProxy, TraceSource trace, IFormatProvider formatProvider)
        {
            HubProxy = Requires.NotNull(hubProxy, nameof(hubProxy));
            Trace = Requires.NotNull(trace, nameof(trace));
            FormatProvider = formatProvider != null ? DataFormatProvider.Create(formatProvider) : null;
        }

        /// <inheritdoc/>
        public IHubProxy HubProxy { get; }

        /// <summary>
        /// Gets the proxy trace.
        /// </summary>
        protected TraceSource Trace { get; }

        /// <summary>
        /// Gets the proxy format provider.
        /// </summary>
        protected IDataFormatProvider FormatProvider { get; }

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
        /// <param name="disposable">Disposable handler.</param>
        protected void AddHubHandler(IDisposable disposable)
        {
            this.onHubHandlers.Add(disposable);
        }
    }
}
