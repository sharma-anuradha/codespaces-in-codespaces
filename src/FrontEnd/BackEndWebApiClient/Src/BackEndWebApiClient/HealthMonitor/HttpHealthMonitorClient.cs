// <copyright file="HttpHealthMonitorClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.HealthMonitor
{
    /// <summary>
    /// Http Client for Health monitor.
    /// </summary>
    public class HttpHealthMonitorClient : HttpClientBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpHealthMonitorClient"/> class.
        /// </summary>
        /// <param name="httpClientProvider">Http Client provider.</param>
        public HttpHealthMonitorClient(
            ICurrentUserHttpClientProvider<BackEndHttpClientProviderOptions> httpClientProvider)
            : base(httpClientProvider)
        {
        }

        // TODO: Eljo add apis here.
    }
}
