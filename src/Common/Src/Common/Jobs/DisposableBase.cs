// <copyright file="DisposableBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Base class to implement the IAsyncDisposable pattern.
    /// </summary>
    public class DisposableBase : IAsyncDisposable
    {
        private readonly CancellationTokenSource disposeCts = new CancellationTokenSource();

        /// <summary>
        /// Gets the disposed token.
        /// </summary>
        protected CancellationToken DisposeToken => this.disposeCts.Token;

        /// <summary>
        /// IAsyncDisposable.DisposeAsync.
        /// </summary>
        /// <returns>Value task.</returns>
        public async ValueTask DisposeAsync()
        {
            this.disposeCts.Cancel();
            await DisposeInternalAsync();
        }

        /// <summary>
        /// Dispose logic.
        /// </summary>
        /// <returns>Completion task.</returns>
        protected virtual Task DisposeInternalAsync()
        {
            return Task.CompletedTask;
        }
    }
}
