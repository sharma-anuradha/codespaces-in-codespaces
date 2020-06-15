// <copyright file="CapacityException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// The base capacity exception.
    /// </summary>
    public abstract class CapacityException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CapacityException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The inner exception.</param>
        public CapacityException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
