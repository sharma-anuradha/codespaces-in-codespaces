// <copyright file="KeyVaultProviderException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts
{
    /// <summary>
    /// KeyVault provider base exception.
    /// </summary>
    public abstract class KeyVaultProviderException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultProviderException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public KeyVaultProviderException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultProviderException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public KeyVaultProviderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
