// <copyright file="ResourceNotFoundException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// A resource broker resource was not found.
    /// </summary>
    public class ResourceNotFoundException : ResourceBrokerException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceNotFoundException"/> class.
        /// </summary>
        /// <param name="resourceId">The resource id.</param>
        /// <param name="inner">The inner exception.</param>
        public ResourceNotFoundException(
            Guid resourceId,
            Exception inner = null)
            : base($"The resource '{resourceId}' was not found.", inner)
        {
            ResourceId = resourceId;
        }

        /// <summary>
        /// Gets the resource id.
        /// </summary>
        public Guid ResourceId { get; }
    }
}
