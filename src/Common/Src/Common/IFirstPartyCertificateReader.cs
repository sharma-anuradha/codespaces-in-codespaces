// <copyright file="IFirstPartyCertificateReader.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.KeyVault;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Reads certificates for first party apps from keyvault.
    /// </summary>
    public interface IFirstPartyCertificateReader
    {
        /// <summary>
        /// Get API First Party App Certificate.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<Certificate> GetApiFirstPartyAppCertificate(IDiagnosticsLogger logger);

        /// <summary>
        /// Get MSI First Party App Certificate.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<Certificate> GetMsiFirstPartyAppCertificate(IDiagnosticsLogger logger);
    }
}