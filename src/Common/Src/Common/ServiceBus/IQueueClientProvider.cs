// <copyright file="IQueueClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus
{
    /// <summary>
    /// Provides access to lazy loaded singleton <see cref="IQueueClient" />.
    /// </summary>
    public interface IQueueClientProvider
    {
        /// <summary>
        /// Gets the <see cref="IQueueClient"/> task.
        /// </summary>
        public Lazy<Task<IQueueClient>> Client { get; }
    }
}
