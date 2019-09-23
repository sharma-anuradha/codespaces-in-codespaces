// <copyright file="ServiceUriBuilder.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common
{
    /// <summary>
    /// Service Uri Builder class.
    /// </summary>
    public class ServiceUriBuilder : IServiceUriBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceUriBuilder"/> class.
        /// </summary>
        /// <param name="developerSettings">Developer settings.</param>
        public ServiceUriBuilder(DeveloperSettings developerSettings)
        {
            DeveloperSettings = Requires.NotNull(developerSettings, nameof(developerSettings));
        }

        private DeveloperSettings DeveloperSettings { get; }

        /// <inheritdoc/>
        public Uri GetCallbackUriFormat(string requestUri, IControlPlaneStampInfo controlPlaneStampInfo)
        {
            var callbackUriBuilder = new UriBuilder(requestUri)
            {
                Query = null,
            };

            callbackUriBuilder.Path = $"{callbackUriBuilder.Path.TrimEnd('/')}/{{0}}/_callback";
            ConstructUri(callbackUriBuilder, controlPlaneStampInfo.DnsHostName);

            return callbackUriBuilder.Uri;
        }

        /// <inheritdoc/>
        public Uri GetServiceUri(string requestUri, IControlPlaneStampInfo controlPlaneStampInfo)
        {
            var serviceUriBuilder = new UriBuilder(requestUri)
            {
                Query = null,
            };

            serviceUriBuilder.Path = serviceUriBuilder.Path.TrimEnd('/');
            ConstructUri(serviceUriBuilder, controlPlaneStampInfo.DnsHostName);

            return serviceUriBuilder.Uri;
        }

        private void ConstructUri(UriBuilder uriBuilder, string dnsHostName)
        {
            var isLocalHost = uriBuilder.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);

            if (!isLocalHost)
            {
                uriBuilder.Scheme = Uri.UriSchemeHttps;
                uriBuilder.Port = -1;
                uriBuilder.Host = dnsHostName; // use location-specific host for callback
            }
            else if (isLocalHost && DeveloperSettings.Enabled)
            {
                uriBuilder.Scheme = Uri.UriSchemeHttp;
                uriBuilder.Port = -1;
                uriBuilder.Host = DeveloperSettings.ForwarderHost;
            }
        }
    }
}
