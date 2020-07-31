// <copyright file="JobPayload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Base class for all the job payloads.
    /// </summary>
    public abstract class JobPayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobPayload"/> class.
        /// </summary>
        protected JobPayload()
        {
            LoggerProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets or sets the logger properties we want to pass into the queue.
        /// </summary>
        public Dictionary<string, object> LoggerProperties { get; set; }
    }

#pragma warning disable SA1402 // File may only contain a single type

    /// <summary>
    /// A job payload that tag a unique type.
    /// </summary>
    /// <typeparam name="T">The type where to generate the tag.</typeparam>
    public class JobPayload<T> : JobPayload
        where T : class
    {
    }

    /// <summary>
    /// A job payload with a content property.
    /// </summary>
    /// <typeparam name="T">Type of the content.</typeparam>
    public class JobContentPayload<T> : JobPayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobContentPayload{T}"/> class.
        /// </summary>
        /// <param name="content">The payload content.</param>
        public JobContentPayload(T content)
        {
            Content = content;
        }

        /// <summary>
        /// Gets the content of this payload.
        /// </summary>
        public T Content { get; }
    }

#pragma warning restore SA1402 // File may only contain a single type

}
