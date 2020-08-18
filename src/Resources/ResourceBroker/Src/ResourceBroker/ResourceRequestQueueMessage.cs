// <copyright file="ResourceRequestQueueMessage.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Resource request message.
    /// </summary>
    public class ResourceRequestQueueMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRequestQueueMessage"/> class.
        /// </summary>
        /// <param name="requestRecordId">Resource Id for request record.</param>
        /// <param name="loggingProperties">logging properties.</param>
        public ResourceRequestQueueMessage(string requestRecordId, IDictionary<string, string> loggingProperties)
        {
            RequestRecordId = requestRecordId;
            LoggingProperties = loggingProperties;
        }

        /// <summary>
        /// Gets or sets the ResourceId for queued request.
        /// </summary>
        public string RequestRecordId { get; set; }

        /// <summary>
        /// Gets or sets the LoggingProperties for queued request.
        /// </summary>
        public IDictionary<string, string> LoggingProperties { get; set; }
    }
}