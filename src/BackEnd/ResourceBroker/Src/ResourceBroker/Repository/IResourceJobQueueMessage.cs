// <copyright file="IResourceJobQueueMessage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository
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

        T GetTypedPayload<T>();
    }
}