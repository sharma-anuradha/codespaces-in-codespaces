// <copyright file="KeyVaultCreationException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider
{
    /// <summary>
    /// KeyVault Creation Exception.
    /// </summary>
    public class KeyVaultCreationException : KeyVaultProviderException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultCreationException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public KeyVaultCreationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultCreationException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public KeyVaultCreationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}