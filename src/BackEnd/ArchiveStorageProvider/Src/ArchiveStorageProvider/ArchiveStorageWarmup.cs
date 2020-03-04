// <copyright file="ArchiveStorageWarmup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider
{
    /// <summary>
    /// Initialize archive storage during service warmup.
    /// </summary>
    public class ArchiveStorageWarmup : IAsyncWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveStorageWarmup"/> class.
        /// </summary>
        /// <param name="archiveStorageProvider">The archive storage provider.</param>
        /// <param name="healthProvider">The service health provider.</param>
        /// <param name="diagnosticsLoggerFactory">The logging factory.</param>
        /// <param name="defaultLogValues">Logging base values.</param>
        public ArchiveStorageWarmup(
            IArchiveStorageProvider archiveStorageProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory diagnosticsLoggerFactory,
            LogValueSet defaultLogValues)
        {
            Requires.NotNull(archiveStorageProvider, nameof(archiveStorageProvider));
            Requires.NotNull(healthProvider, nameof(healthProvider));
            Requires.NotNull(diagnosticsLoggerFactory, nameof(diagnosticsLoggerFactory));
            Requires.NotNull(defaultLogValues, nameof(defaultLogValues));

            // Warmup requires access to the implementation internals.
            // If a mock or stub IArchiveStorageProvider implementation is given, the warmup will be a no-op.
            ArchiveStorageProvider = archiveStorageProvider as ArchiveStorageProvider;
            DiagnosticsLogger = diagnosticsLoggerFactory.New(defaultLogValues);
        }

        private ArchiveStorageProvider ArchiveStorageProvider { get; }

        private IHealthProvider HealthProvider { get; }

        private IDiagnosticsLogger DiagnosticsLogger { get; }

        /// <inheritdoc/>
        public async Task WarmupCompletedAsync()
        {
            if (ArchiveStorageProvider != null)
            {
                try
                {
                    await ArchiveStorageProvider.WarmupCompletedAsync();
                }
                catch (Exception ex)
                {
                    HealthProvider.MarkUnhealthy(ex, DiagnosticsLogger);
                }
            }
        }
    }
}
