// <copyright file="RegionalDocumentDbClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// A regional document db client provider.
    /// </summary>
    public class RegionalDocumentDbClientProvider : DocumentDbClientProvider, IRegionalDocumentDbClientProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegionalDocumentDbClientProvider"/> class.
        /// </summary>
        /// <param name="clientOptions">The client options.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public RegionalDocumentDbClientProvider(
            IOptions<RegionalDocumentDbClientOptions> clientOptions,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(clientOptions, healthProvider, loggerFactory, defaultLogValues)
        {
        }
    }
}
