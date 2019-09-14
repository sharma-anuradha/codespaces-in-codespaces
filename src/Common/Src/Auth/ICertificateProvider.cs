// <copyright file="ICertificateProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Provides primary and secondary certificates.
    /// </summary>
    public interface ICertificateProvider
    {
        /// <summary>
        /// Provides valid certificates.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <returns>Valid certificates object.</returns>
        Task<ValidCertificates> GetValidCertificatesAsync(IDiagnosticsLogger logger);
    }
}
