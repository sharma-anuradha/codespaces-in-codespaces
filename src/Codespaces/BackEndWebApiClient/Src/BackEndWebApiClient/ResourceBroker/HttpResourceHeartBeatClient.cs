// <copyright file="HttpResourceHeartBeatClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.ResourceBroker;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker
{
    /// <summary>
    /// Http client for sending heartbeats to the Backend.
    /// </summary>
    public class HttpResourceHeartBeatClient : HttpClientBase<BackEndHttpClientProviderOptions>, IResourceHeartBeatHttpContract
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResourceHeartBeatClient"/> class.
        /// </summary>
        /// <param name="httpClientProvider">The backend http client provider.</param>
        public HttpResourceHeartBeatClient(
            IHttpClientProvider<BackEndHttpClientProviderOptions> httpClientProvider)
            : base(httpClientProvider)
        {
        }

        /// <inheritdoc/>
        public async Task UpdateHeartBeatAsync(Guid resourceId, HeartBeatBody heartBeat, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            Requires.NotEmpty(resourceId, nameof(resourceId));
            var requestUri = ResourceHeartBeatHttpContract.GetUpdateHeartBeatUri(resourceId);
            await SendRawAsync<HeartBeatBody>(ResourceHeartBeatHttpContract.UpdateHeartBeatMethod, requestUri, heartBeat, logger);
        }
    }
}
