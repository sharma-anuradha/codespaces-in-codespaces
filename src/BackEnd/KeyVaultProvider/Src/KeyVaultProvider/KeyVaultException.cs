// <copyright file="KeyVaultException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider
{
    /// <summary>
    /// KeyVault Exception.
    /// </summary>
    [Serializable]
    internal class KeyVaultException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public KeyVaultException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public KeyVaultException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}