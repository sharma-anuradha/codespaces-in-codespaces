// <copyright file="CloudEnvironmentException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Base exception for the resource broker subsystem.
    /// </summary>
    public abstract class CloudEnvironmentException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The inner exception.</param>
        public CloudEnvironmentException(string message, Exception inner = null)
            : base(message, inner)
        {
        }
    }
}
