// <copyright file="ISessionClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus
{
    /// <summary>
    /// Provides access to lazy loaded singleton <see cref="ISessionClient" />.
    /// </summary>
    public interface ISessionClientProvider
    {
        /// <summary>
        /// Gets the <see cref="ISessionClient"/> task.
        /// </summary>
        public Lazy<Task<ISessionClient>> Client { get; }
    }
}
