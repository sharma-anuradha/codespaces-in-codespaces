// <copyright file="ExportStorageWarmup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider
{
    /// <summary>
    /// Initialize export storage during service warmup.
    /// </summary>
    public class ExportStorageWarmup : IAsyncWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportStorageWarmup"/> class.
        /// </summary>
        /// <param name="exportStorageProvider">The export storage provider.</param>
        /// <param name="healthProvider">The service health provider.</param>
        /// <param name="diagnosticsLoggerFactory">The logging factory.</param>
        /// <param name="defaultLogValues">Logging base values.</param>
        public ExportStorageWarmup(
            IExportStorageProvider exportStorageProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory diagnosticsLoggerFactory,
            LogValueSet defaultLogValues)
        {
            // Warmup requires access to the implementation internals.
            // If a mock or stub IExportStorageProvider implementation is given, the warmup will be a no-op.
            ExportStorageProvider = Requires.NotNull(exportStorageProvider, nameof(exportStorageProvider)) as ExportStorageProvider;
            Requires.NotNull(healthProvider, nameof(healthProvider));
            Requires.NotNull(diagnosticsLoggerFactory, nameof(diagnosticsLoggerFactory));
            Requires.NotNull(defaultLogValues, nameof(defaultLogValues));

            DiagnosticsLogger = diagnosticsLoggerFactory.New(defaultLogValues);
        }

        private ExportStorageProvider ExportStorageProvider { get; }

        private IHealthProvider HealthProvider { get; }

        private IDiagnosticsLogger DiagnosticsLogger { get; }

        /// <inheritdoc/>
        public async Task WarmupCompletedAsync()
        {
            if (ExportStorageProvider != null)
            {
                try
                {
                    await ExportStorageProvider.WarmupCompletedAsync();
                }
                catch (Exception ex)
                {
                    HealthProvider.MarkUnhealthy(ex, DiagnosticsLogger);
                }
            }
        }
    }
}
