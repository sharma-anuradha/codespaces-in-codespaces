// <copyright file="ManagedIdentityProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.BatchAI.Fluent.Models;
using Microsoft.Identity.Client;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ManagedIdentityProvider
{
    /// <summary>
    /// Provides access to the Azure Managed Identity Resource Provider.
    /// </summary>
    public class ManagedIdentityProvider : HttpClientBase, IManagedIdentityProvider
    {
        private const string LogBaseName = "managed_identity_provider";

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedIdentityProvider"/> class.
        /// </summary>
        /// <param name="httpClientProvider">The HTTP client provider.</param>
        /// <param name="logger">The logger.</param>
        public ManagedIdentityProvider(
            IManagedIdentityHttpClientProvider httpClientProvider)
            : base(httpClientProvider)
        {
        }

        /// <inheritdoc/>
        public Task<CredentialResponse> GetSystemAssignedCredentialsAsync(string identityUrl, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_get",
                async (childLogger) =>
                {
                    return await SendAsync<object, CredentialResponse>(HttpMethod.Get, identityUrl, null, logger);
                });
        }
    }
}
