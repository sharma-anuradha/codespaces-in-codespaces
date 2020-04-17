// <copyright file="ImagesHttpClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Images;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.Images
{
    /// <inheritdoc/>
    public class ImagesHttpClient : HttpClientBase, IImagesHttpClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImagesHttpClient"/> class.
        /// </summary>
        /// <param name="httpClientProvider">Http Client provider.</param>
        public ImagesHttpClient(
            IHttpClientProvider<BackEndHttpClientProviderOptions> httpClientProvider)
            : base(httpClientProvider)
        {
        }

        /// <inheritdoc/>
        public Task<string> GetAsync(
            ImageFamilyType imageType,
            string family,
            string property,
            string defaultValue,
            IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(family, nameof(family));
            Requires.NotNullOrEmpty(property, nameof(property));
            Requires.NotNullOrEmpty(defaultValue, nameof(defaultValue));

            var requestUri = ImagesHttpContract.GetImageUri(imageType, family, property, defaultValue);
            return Retry.DoAsync(async (attemptNumber) =>
            {
                var result = await SendAsync<string, string>(ImagesHttpContract.GetImageHttpMethod, requestUri, null, logger.NewChildLogger());
                return result;
            });
        }
    }
}
