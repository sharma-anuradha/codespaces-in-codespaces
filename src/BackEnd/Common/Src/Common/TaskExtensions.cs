// <copyright file="TaskExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Returns an awaitable task which is configured to continue on any context.
        /// </summary>
        public static ConfiguredTaskAwaitable<T> ContinueOnAnyContext<T>(this Task<T> task)
        {
            Requires.NotNull(task, nameof(task));
            return task.ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <summary>
        /// Returns an awaitable task which is configured to continue on any context.
        /// </summary>
        public static ConfiguredTaskAwaitable ContinueOnAnyContext(this Task task)
        {
            Requires.NotNull(task, nameof(task));
            return task.ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
