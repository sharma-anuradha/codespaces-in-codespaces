// <copyright file="NotFoundException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts
{
    /// <summary>
    /// Not found exception.
    /// </summary>
    public class NotFoundException : KeyVaultProviderException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The inner exception.</param>
        public NotFoundException(string message, Exception inner = null)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Gets the resource id.
        /// </summary>
        public Guid ResourceId { get; }
    }
}