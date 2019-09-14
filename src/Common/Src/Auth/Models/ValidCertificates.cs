// <copyright file="ValidCertificates.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Models
{
    /// <summary>
    /// Represents valid certificates.
    /// </summary>
    public class ValidCertificates
    {
        /// <summary>
        /// Gets or sets the primary certificate.
        /// This should be used for token generation. It is usually the latest and current.
        /// </summary>
        public Certificate Primary
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the seconardary certificates.
        /// This can be used for token validation.
        /// </summary>
        public Certificate[] Secondaries
        {
            get;
            set;
        }
    }
}
