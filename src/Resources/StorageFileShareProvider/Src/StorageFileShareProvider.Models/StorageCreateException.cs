// <copyright file="StorageCreateException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// Exception of errors during storage creation.
    /// </summary>
    public class StorageCreateException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageCreateException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public StorageCreateException(string message)
            : base(string.Format("Storage creation failed: {0}", message))
        {
        }
    }
}
