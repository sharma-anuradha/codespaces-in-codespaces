// <copyright file="DistributedLease.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Disposable that triggers a given action on callback.
    /// </summary>
    public class ActionDisposable : IDisposable
    {
        private ActionDisposable(Action action)
        {
            Action = action;
        }

        private Action Action { get; set; }

        /// <summary>
        /// Creates an ActionDisposable.
        /// </summary>
        /// <param name="action">Action that should be triggered on dispose.</param>
        /// <returns>Returns the disposable that wraps the callback.</returns>
        public static IDisposable Create(Action action)
        {
            return new ActionDisposable(action);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Action?.Invoke();
            Action = null;
        }
    }
}
