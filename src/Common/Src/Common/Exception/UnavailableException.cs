// <copyright file="UnavailableException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Unavailable exception.
    /// </summary>
    public class UnavailableException : CodedException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnavailableException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public UnavailableException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnavailableException"/> class.
        /// </summary>
        /// <param name="messageCode">Target message code.</param>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public UnavailableException(int messageCode, string message = null, Exception innerException = null)
            : base(messageCode, message ?? $"Unavailable '{messageCode}' error occured.", innerException)
        {
        }
    }
}
