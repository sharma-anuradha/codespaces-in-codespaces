// <copyright file="IResourceJobQueueMessage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    ///
    /// </summary>
    public interface IResourceJobQueueMessage
    {
        /// <summary>
        ///
        /// </summary>
        string Content { get; }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetTypedPayload<T>();
    }
}