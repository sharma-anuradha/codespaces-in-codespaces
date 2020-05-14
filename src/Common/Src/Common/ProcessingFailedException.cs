// <copyright file="ProcessingFailedException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Processing failing exception.
    /// General exception thrown when an action is failed processing.
    /// </summary>
    public class ProcessingFailedException : CodedException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessingFailedException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public ProcessingFailedException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessingFailedException"/> class.
        /// </summary>
        /// <param name="messageCode">Target message code.</param>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public ProcessingFailedException(int messageCode, string message = null, Exception innerException = null)
            : base(messageCode, message ?? $"Processing failed '{messageCode}' error occured.", innerException)
        {
        }
    }
}
