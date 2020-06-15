// <copyright file="CloudEnvironmentNotFoundException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// A resource broker resource was not found.
    /// </summary>
    public class CloudEnvironmentNotFoundException : CloudEnvironmentException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentNotFoundException"/> class.
        /// </summary>
        /// <param name="resourceId">The resource id.</param>
        /// <param name="inner">The inner exception.</param>
        public CloudEnvironmentNotFoundException(
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
