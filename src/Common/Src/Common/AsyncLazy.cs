// <copyright file="AsyncLazy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Enable the Lazy instance to be loaded Asynchronously. See the following for more info: https://blogs.msdn.microsoft.com/pfxteam/2011/01/15/asynclazyt/.
    /// </summary>
    /// <typeparam name="T">type of instance to load asynchronously.</typeparam>
    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLazy{T}"/> class.
        /// </summary>
        /// <param name="valueFactory">value factory.</param>
        public AsyncLazy(Func<T> valueFactory)
            : base(() => Task.Factory.StartNew(valueFactory))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncLazy{T}"/> class.
        /// </summary>
        /// <param name="taskFactory">task factory.</param>
        public AsyncLazy(Func<Task<T>> taskFactory)
            : base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap())
        {
        }

        /// <summary>
        /// Automatically allow users to await on the instance of the lazy value instead of calling await instance.Value.
        /// </summary>
        /// <returns>task awaiter.</returns>
        public TaskAwaiter<T> GetAwaiter()
        {
            return Value.GetAwaiter();
        }
    }
}
