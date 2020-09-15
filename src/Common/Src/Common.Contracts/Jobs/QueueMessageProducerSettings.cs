// <copyright file="QueueMessageProducerSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Settings for the Queue message producer.
    /// </summary>
    public class QueueMessageProducerSettings
    {
        /// <summary>
        /// Default message count.
        /// </summary>
        public const int DefaultMessageCount = 5;

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
            new QueueMessageProducerSettings(DefaultMessageCount, DefaultVisibilityTimeout, DefaultTimeout, null);

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueMessageProducerSettings"/> class.
        /// </summary>
        /// <param name="messageCount">The message count.</param>
        /// <param name="visibilityTimeout">The visibility timeout.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="messageOptions">Message dataflow block options.</param>
        public QueueMessageProducerSettings(int messageCount, TimeSpan visibilityTimeout, TimeSpan timeout, DataflowBlockOptions messageOptions = null)
        {
            MessageCount = messageCount;
            VisibilityTimeout = visibilityTimeout;
            Timeout = timeout;
            MessageOptions = messageOptions;
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

        /// <summary>
        /// Gets the message dataflow block options.
        /// </summary>
        public DataflowBlockOptions MessageOptions { get; }

        public static QueueMessageProducerSettings Create(
            int? messageCount = null,
            TimeSpan? visibilityTimeout = null,
            TimeSpan? timeout = null,
            DataflowBlockOptions messageOptions = null)
        {
            return new QueueMessageProducerSettings(
                messageCount ?? DefaultMessageCount,
                visibilityTimeout ?? DefaultVisibilityTimeout,
                timeout ?? DefaultTimeout,
                messageOptions);
        }
    }
}
