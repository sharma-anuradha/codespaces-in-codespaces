// <copyright file="QueueResourceJobQueueMessage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Azure.Storage.Queue;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.AzureQueue
{
    /// <summary>
    /// 
    /// </summary>
    public class QueueResourceJobQueueMessage : IResourceJobQueueMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueResourceJobQueueMessage"/> class.
        /// </summary>
        /// <param name="message"></param>
        public QueueResourceJobQueueMessage(CloudQueueMessage message)
        {
            Message = message;
        }

        /// <inheritdoc/>
        public string Content
        {
            get { return Message.AsString; }
        }

        /// <summary>
        /// Gets the underlying Azure Queue message.
        /// </summary>
        public CloudQueueMessage Message { get; }

        /// <inheritdoc/>
        public T GetTypedPayload<T>()
        {
            return JsonConvert.DeserializeObject<T>(Message.AsString);
        }
    }
}
