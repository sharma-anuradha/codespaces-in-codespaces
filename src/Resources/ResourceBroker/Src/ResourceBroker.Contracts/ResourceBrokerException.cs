// <copyright file="ResourceBrokerException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
{
    /// <summary>
    /// Base exception for the resource broker subsystem.
    /// </summary>
    public abstract class ResourceBrokerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceBrokerException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The inner exception.</param>
        public ResourceBrokerException(string message, Exception inner = null)
            : base(message, inner)
        {
        }
    }
}
