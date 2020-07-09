// <copyright file="LogHubMessageHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace DiagnosticsServer.Hubs
{
    /// <summary>
    /// Manages subscriptions to <see cref="LogHub"/> events.
    /// </summary>
    public class LogHubMessageHandler : ILogHubEventSource, ILogHubSubscriptions
    {
        private readonly ConcurrentDictionary<string, DisposableCallback<Func<Task>>> reloadLogsTasks = new ConcurrentDictionary<string, DisposableCallback<Func<Task>>>();

        /// <inheritdoc/>
        IDisposable ILogHubSubscriptions.OnReloadLogs(Func<Task> action)
        {
            var id = Guid.NewGuid().ToString();
            var callback = new DisposableCallback<Func<Task>>(action, () => this.reloadLogsTasks.TryRemove(id, out var _));
            reloadLogsTasks.TryAdd(id, callback);
            return callback;
        }

        /// <inheritdoc/>
        Task ILogHubEventSource.OnReloadLogs()
        {
            return Task.WhenAll(this.reloadLogsTasks.Values.Select(x => x.Callback()));
        }

        private class DisposableCallback<T> : IDisposable
        {
            private readonly Action onDispose;

            public DisposableCallback(T callback, Action onDispose)
            {
                this.Callback = callback;
                this.onDispose = onDispose;
            }

            public T Callback { get; private set; }

            public void Dispose()
            {
                this.onDispose();
            }
        }
    }
}