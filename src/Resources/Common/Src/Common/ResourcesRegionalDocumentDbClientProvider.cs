// <copyright file="ResourcesRegionalDocumentDbClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Backend.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common
{
    /// <summary>
    /// A resources document db client provider.
    /// </summary>
    public class ResourcesRegionalDocumentDbClientProvider : DocumentDbClientProvider, IResourcesRegionalDocumentDbClientProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcesRegionalDocumentDbClientProvider"/> class.
        /// </summary>
        /// <param name="clientOptions">The client options.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public ResourcesRegionalDocumentDbClientProvider(
            IOptions<DocumentDbClientOptions> clientOptions,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(clientOptions, healthProvider, loggerFactory, defaultLogValues)
        {
        }
    }
}
