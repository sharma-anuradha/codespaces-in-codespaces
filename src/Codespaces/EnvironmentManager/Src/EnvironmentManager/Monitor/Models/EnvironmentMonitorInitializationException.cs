// <copyright file="EnvironmentMonitorInitializationException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment monitoring exception..
    /// </summary>
    internal class EnvironmentMonitorInitializationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentMonitorInitializationException"/> class.
        /// </summary>
        /// <param name="environmentId">The resource id.</param>
        /// <param name="inner">The inner exception.</param>
        public EnvironmentMonitorInitializationException(
            string environmentId,
            Exception inner = null)
            : base($"Failed to initialize monitoring for environment {environmentId}", inner)
        {
            EnvironmentId = environmentId;
        }

        /// <summary>
        /// Gets the resource id.
        /// </summary>
        public string EnvironmentId { get; }
    }
}