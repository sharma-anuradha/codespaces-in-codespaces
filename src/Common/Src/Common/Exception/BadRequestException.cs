// <copyright file="BadRequestException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// BadRequestException Exception.
    /// </summary>
    public class BadRequestException : CodedException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BadRequestException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public BadRequestException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BadRequestException"/> class.
        /// </summary>
        /// <param name="messageCode">Target message code.</param>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public BadRequestException(int messageCode, string message = null, Exception innerException = null)
            : base(messageCode, message == null ? $"Bad request '{messageCode}' error occured." : message, innerException)
        {
        }
    }
}
