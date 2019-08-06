﻿// <copyright file="HubProxy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

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
        /// <param name="connection">The hub connection</param>
        /// <param name="hubName">Optional name of the hub.</param>
        public HubProxy(HubConnection connection, string hubName)
        {
            Connection = Requires.NotNull(connection, nameof(connection));
            this.hubName = hubName;
        }

        /// <summary>
        /// Gets the underlying hub connection.
        /// </summary>
        public HubConnection Connection { get; }

        /// <summary>
        /// Create a hub proxy of a type.
        /// </summary>
        /// <typeparam name="T">Type of the proxy to create.</typeparam>
        /// <param name="hubConnection">The hub connection instance.</param>
        /// <param name="trace">Trace instance.</param>
        /// <param name="useSignalRHub">If using the signalR hub</param>
        /// <returns>Instance of the proxy</returns>
        public static T CreateHubProxy<T>(HubConnection hubConnection, TraceSource trace, bool useSignalRHub = false)
        {
            var hubProxy = new HubProxy(hubConnection, useSignalRHub ? (string)typeof(T).GetField("HubName").GetValue(null) : null);
            return (T)Activator.CreateInstance(typeof(T), hubProxy, trace);
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
