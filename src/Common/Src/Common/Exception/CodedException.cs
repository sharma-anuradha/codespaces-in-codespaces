// <copyright file="CodedException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Base Coded Exception.
    /// </summary>
    public abstract class CodedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodedException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public CodedException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodedException"/> class.
        /// </summary>
        /// <param name="messageCode">Target message code.</param>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public CodedException(int messageCode, string message, Exception innerException = null)
            : this(message, innerException)
        {
            MessageCode = messageCode;
        }

        /// <summary>
        /// Gets message code.
        /// </summary>
        public int MessageCode { get; }
    }
}
