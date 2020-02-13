// <copyright file="HubProxy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Our client proxy base class.
    /// </summary>
    public class HubProxy : IHubProxy
    {
        private const string InvokeHubMethodAsync = "InvokeHubMethodAsync";

        private readonly string hubName;

        /// <summary>
        /// Initializes a new instance of the <see cref="HubProxy"/> class.
        /// </summary>
        /// <param name="hubClient">The hub client.</param>
        /// <param name="hubName">Optional name of the hub.</param>
        public HubProxy(HubClient hubClient, string hubName)
        {
            Client = Requires.NotNull(hubClient, nameof(hubClient));
            this.hubName = hubName;
        }

        /// <inheritdoc/>
        public event AsyncEventHandler ConnectionStateChanged
        {
            add
            {
                Client.ConnectionStateChanged += value;
            }

            remove
            {
                Client.ConnectionStateChanged -= value;
            }
        }

        /// <summary>
        /// Gets the underlying hub connection.
        /// </summary>
        public HubConnection Connection => Client.Connection;

        /// <inheritdoc/>
        public bool IsConnected => Client.IsConnected;

        private HubClient Client { get; }

        /// <summary>
        /// Create a hub proxy of a type.
        /// </summary>
        /// <typeparam name="T">Type of the proxy to create.</typeparam>
        /// <param name="hubClient">The hub client.</param>
        /// <param name="trace">Trace instance.</param>
        /// <param name="useSignalRHub">If using the signalR hub.</param>
        /// <returns>Instance of the proxy.</returns>
        public static T CreateHubProxy<T>(HubClient hubClient, TraceSource trace, bool useSignalRHub = false)
        {
            return CreateHubProxy<T>(hubClient, trace, null, useSignalRHub);
        }

        /// <summary>
        /// Create a hub proxy of a type.
        /// </summary>
        /// <typeparam name="T">Type of the proxy to create.</typeparam>
        /// <param name="hubClient">The hub client.</param>
        /// <param name="trace">Trace instance.</param>
        /// <param name="formatProvider">Optional format provider.</param>
        /// <param name="useSignalRHub">If using the signalR hub.</param>
        /// <returns>Instance of the proxy.</returns>
        public static T CreateHubProxy<T>(HubClient hubClient, TraceSource trace, IFormatProvider formatProvider, bool useSignalRHub = false)
        {
            var hubProxy = new HubProxy(hubClient, useSignalRHub ? (string)typeof(T).GetField("HubName").GetValue(null) : null);
            return (T)Activator.CreateInstance(typeof(T), hubProxy, trace, formatProvider);
        }

        /// <inheritdoc/>
        public IDisposable On(string methodName, Type[] parameterTypes, Func<object[], Task> handler)
        {
            return Connection.On(ToHubMethodName(methodName), parameterTypes, handler);
        }

        /// <inheritdoc/>
        public async Task<T> InvokeAsync<T>(string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(this.hubName))
            {
                return (T)(await Connection.InvokeCoreAsync(methodName, typeof(T), args, cancellationToken).ConfigureAwait(false));
            }
            else
            {
                return (T)(await Connection.InvokeCoreAsync(InvokeHubMethodAsync, typeof(T), new object[] { ToHubMethodName(methodName), args }, cancellationToken));
            }
        }

        /// <inheritdoc/>
        public Task InvokeAsync(string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(this.hubName))
            {
                return Connection.InvokeCoreAsync(methodName, typeof(object), args, cancellationToken);
            }
            else
            {
                return Connection.InvokeCoreAsync(InvokeHubMethodAsync, typeof(object), new object[] { ToHubMethodName(methodName), args }, cancellationToken);
            }
        }

        private string ToHubMethodName(string methodName)
        {
            return string.IsNullOrEmpty(this.hubName) ? methodName : $"{this.hubName}.{methodName}";
        }
    }
}
