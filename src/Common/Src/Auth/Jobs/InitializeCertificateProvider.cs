// <copyright file="InitializeCertificateProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Jobs
{
    /// <summary>
    /// Initializes certificate provider through warmup.
    /// </summary>
    public class InitializeCertificateProvider : IAsyncWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeCertificateProvider"/> class.
        /// </summary>
        /// <param name="certificateProvider">Certificate provider.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        /// <param name="defaultLogValues">default log values.</param>
        public InitializeCertificateProvider(
            ICertificateProvider certificateProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
        {
            Requires.NotNull(loggerFactory, nameof(loggerFactory));

            var logger = loggerFactory.New(defaultLogValues);
            InitializeCertificateLoader = CertificateLoader(certificateProvider, logger);
        }

        private Task InitializeCertificateLoader { get; }

        /// <inheritdoc/>
        public async Task WarmupCompletedAsync()
        {
            await InitializeCertificateLoader;
        }

        private async Task<ValidCertificates> CertificateLoader(ICertificateProvider certificateProvider, IDiagnosticsLogger logger)
        {
            var initializationDuration = logger.StartDuration();
            try
            {
                var certificates = await certificateProvider.GetValidCertificatesAsync(logger);
                logger.AddDuration(initializationDuration)
                        .LogInfo("certificate_initialization_in_warmup_success");

                return certificates;
            }
            catch (Exception e)
            {
                logger.AddDuration(initializationDuration)
                    .LogException($"certificate_initialization_in_warmup_failed", e);

                throw;
            }
        }
    }
}
