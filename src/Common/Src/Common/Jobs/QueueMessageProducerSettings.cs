// <copyright file="QueueMessageProducerSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Settings for the Queue message producer.
    /// </summary>
    public class QueueMessageProducerSettings
    {
        /// <summary>
        /// Gets the default timeout.
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets the default visibility timeout.
        /// </summary>
        public static readonly TimeSpan DefaultVisibilityTimeout = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets the default settings.
        /// </summary>
        public static readonly QueueMessageProducerSettings Default =
            new QueueMessageProducerSettings(5, DefaultVisibilityTimeout, DefaultTimeout);

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueMessageProducerSettings"/> class.
        /// </summary>
        /// <param name="messageCount">The message count.</param>
        /// <param name="visibilityTimeout">The visibility timeout.</param>
        /// <param name="timeout">The timeout.</param>
        public QueueMessageProducerSettings(int messageCount, TimeSpan visibilityTimeout, TimeSpan timeout)
        {
            MessageCount = messageCount;
            VisibilityTimeout = visibilityTimeout;
            Timeout = timeout;
        }

        /// <summary>
        /// Gets the message count.
        /// </summary>
        public int MessageCount { get; }

        /// <summary>
        /// Gets the visibility timeout.
        /// </summary>
        public TimeSpan VisibilityTimeout { get; }

        /// <summary>
        /// Gets the timeout.
        /// </summary>
        public TimeSpan Timeout { get; }
    }
}
