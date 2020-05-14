// <copyright file="EntityNotFoundException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Entity Not Found Exception.
    /// </summary>
    public class EntityNotFoundException : CodedException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public EntityNotFoundException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
        /// </summary>
        /// <param name="messageCode">Target message code.</param>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public EntityNotFoundException(int messageCode, string message = null, Exception innerException = null)
            : base(messageCode, message ?? $"Not found '{messageCode}' error occured.", innerException)
        {
        }
    }
}
