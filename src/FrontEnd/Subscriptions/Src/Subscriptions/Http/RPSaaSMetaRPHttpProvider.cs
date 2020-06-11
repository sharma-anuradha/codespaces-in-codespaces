﻿// <copyright file="RPSaaSMetaRPHttpProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.KeyVault;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http
{
    /// <inheritdoc/>
    public class RPSaaSMetaRPHttpProvider<TOptions>
        : IHttpClientProvider<TOptions>
        where TOptions : class, IHttpClientProviderOptions, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RPSaaSMetaRPHttpProvider{TOptions}"/> class.
        /// </summary>
        /// <param name="options">The options instance.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="keyVaultSecretReader">the key vault secret reader.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="controlPlaneInfo">Control plane information used to get the KV.</param>
        /// <param name="firstPartyAppSettings">First Party Add settings.</param>
        /// <param name="productInfo">Product info for the current service, to be added to.
        /// request headers.</param>
        public RPSaaSMetaRPHttpProvider(
            IOptions<TOptions> options,
            IKeyVaultSecretReader keyVaultSecretReader,
            IDiagnosticsLogger logger,
            IControlPlaneInfo controlPlaneInfo,
            FirstPartyAppSettings firstPartyAppSettings,
            ProductInfoHeaderValue productInfo)
        {
            Requires.NotNull(options, nameof(options));
            Requires.NotNull(productInfo, nameof(productInfo));
            Requires.NotNull(firstPartyAppSettings, nameof(firstPartyAppSettings));

            HttpMessageHandler httpHandlerChain = new HttpClientHandler();
            httpHandlerChain = new FPAAccessTokenHandler(httpHandlerChain, keyVaultSecretReader, controlPlaneInfo, firstPartyAppSettings, logger);
            httpHandlerChain = new ProductInfoHeaderHandler(httpHandlerChain, productInfo);

            HttpClient = new HttpClient(httpHandlerChain)
            {
                BaseAddress = new Uri(options.Value.BaseAddress),
            };
        }

        /// <inheritdoc/>
        public HttpClient HttpClient { get; }
    }
}
