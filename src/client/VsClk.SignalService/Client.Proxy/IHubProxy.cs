// <copyright file="IHubProxy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Hub method options.
    /// </summary>
    public enum HubMethodOption
    {
        /// <summary>
        /// Use send.
        /// </summary>
        Send,

        /// <summary>
        /// Use invoke.
        /// </summary>
        Invoke,
    }

    /// <summary>
    /// Definition of a hub proxy.
    /// </summary>
    public interface IHubProxy : IHubProxyConnection
    {
        /// <summary>
        /// To receive notifications.
        /// </summary>
        /// <param name="methodName">The method name.</param>
        /// <param name="parameterTypes">Types of matchig arguments.</param>
        /// <param name="handler">The handler to call when the notification arrives</param>
        /// <returns>Disposable object.</returns>
        IDisposable On(string methodName, Type[] parameterTypes, Func<object[], Task> handler);

        /// <summary>
        /// Invoke a a hub method.
        /// </summary>
        /// <typeparam name="T">Type of the result.</typeparam>
        /// <param name="methodName">Hub method name.</param>
        /// <param name="args">Arguments for the hub method.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Result from the hub method invocation.</returns>
        Task<T> InvokeAsync<T>(string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Invoke a a hub method.
        /// </summary>
        /// <param name="methodName">Hub method name.</param>
        /// <param name="args">Arguments for the hub method.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task to wait.</returns>
        Task SendAsync(string methodName, object[] args, CancellationToken cancellationToken = default(CancellationToken));
    }
}
