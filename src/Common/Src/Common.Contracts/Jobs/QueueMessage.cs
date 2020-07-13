// <copyright file="QueueMessage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Queue message instance.
    /// </summary>
    public abstract class QueueMessage
    {
        /// <summary>
        /// Gets  the id of this message.
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// Gets or sets the content of this message.
        /// </summary>
        public abstract byte[] Content { get; set; }
    }
}
