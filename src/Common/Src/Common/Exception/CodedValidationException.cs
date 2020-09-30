// <copyright file="CodedValidationException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Validation exception with error code.
    /// </summary>
    public class CodedValidationException : CodedException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodedValidationException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public CodedValidationException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodedValidationException"/> class.
        /// </summary>
        /// <param name="messageCode">Target message code.</param>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public CodedValidationException(int messageCode, string message = null, Exception innerException = null)
            : base(messageCode, message ?? $"Validation '{messageCode}' error occured.", innerException)
        {
        }
    }
}
