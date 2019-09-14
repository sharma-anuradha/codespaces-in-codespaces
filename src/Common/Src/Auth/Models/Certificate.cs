// <copyright file="Certificate.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Models
{
    /// <summary>
    /// Certificate model to hold raw certificates.
    /// </summary>
    public class Certificate
    {
        /// <summary>
        /// Gets or sets the unique identifier of the certificate.
        /// </summary>
        public string Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the raw data of the cerficate.
        /// </summary>
        public byte[] RawBytes
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the date and time at which the certificate expires.
        /// </summary>
        public DateTime ExpiresAt
        {
            get;
            set;
        }
    }
}
