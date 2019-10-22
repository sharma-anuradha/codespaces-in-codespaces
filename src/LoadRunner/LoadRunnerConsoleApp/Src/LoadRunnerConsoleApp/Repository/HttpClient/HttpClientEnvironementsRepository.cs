// <copyright file="HttpClientEnvironementsRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.HttpClient.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.HttpClient
{
    /// <summary>
    /// Repository that gates access to users environments.
    /// </summary>
    public class HttpClientEnvironementsRepository : HttpClientBase, IEnvironementsRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientEnvironementsRepository"/> class.
        /// </summary>
        /// <param name="appSettings">Target app settings.</param>
        /// <param name="currentUserHttpClientProvider">Target current user http client provider.</param>
        public HttpClientEnvironementsRepository(
            AppSettings appSettings,
            ICurrentUserHttpClientProvider currentUserHttpClientProvider)
            : base(currentUserHttpClientProvider)
        {
            currentUserHttpClientProvider.Initalize(appSettings.EnvironmentsBaseUri);
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "http_client_environements";

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironmentResult>> ListEnvironmentsAsync(IDiagnosticsLogger logger)
        {
            var requestUri = HttpContractEnvironements.GetListEnvironmentUri();
            var result = await SendAsync<string, IEnumerable<CloudEnvironmentResult>>(
                HttpContractEnvironements.ListEnvironmentsMethod, requestUri, null, logger.WithValues(new LogValueSet()));
            return result;
        }

        /// <inheritdoc/>
        public async Task DeleteEnvironmentAsync(Guid id, IDiagnosticsLogger logger)
        {
            var requestUri = HttpContractEnvironements.GetDeleteEnvironmentUri(id);
            await SendAsync<string, string>(
                HttpContractEnvironements.DeleteEnvironmentsMethod, requestUri, null, logger.WithValues(new LogValueSet()));
        }

        /// <inheritdoc/>
        public async Task<CloudEnvironmentResult> ProvisionEnvironmentAsync(
            string planId, string environmentName, string gitRepo, string location, string skuName, IDiagnosticsLogger logger)
        {
            var body = new CreateCloudEnvironmentBody()
            {
                PlanId = planId,
                FriendlyName = environmentName,
                Location = location,
                SkuName = skuName,
                Seed = new SeedInfoBody
                {
                    SeedType = "git",
                    SeedMoniker = gitRepo,
                },
                Type = "cloudEnvironment",
            };

            var requestUri = HttpContractEnvironements.GetCreateEnvironmentUri();
            var result = await SendAsync<CreateCloudEnvironmentBody, CloudEnvironmentResult>(
                HttpContractEnvironements.CreateEnvironmentsMethod, requestUri, body, logger.WithValues(new LogValueSet()));
            return result;
        }
    }
}
