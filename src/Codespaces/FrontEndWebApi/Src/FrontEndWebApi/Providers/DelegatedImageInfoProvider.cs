// <copyright file="DelegatedImageInfoProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.Images;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Providers
{
    /// <inheritdoc/>
    public class DelegatedImageInfoProvider : ICurrentImageInfoProvider
    {
        private const string PropertyName = "name";
        private const string PropertyVersion = "version";
        private const string LogBaseName = "delegated_image_info_provider";

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegatedImageInfoProvider"/> class.
        /// </summary>
        /// <param name="imagesHttpClient">The http client to use for making
        /// delegated requests to the backend.</param>
        /// <param name="cache">The cache to use for caching responses.</param>
        public DelegatedImageInfoProvider(
            IImagesHttpClient imagesHttpClient,
            IManagedCache cache)
        {
            Requires.NotNull(imagesHttpClient, nameof(imagesHttpClient));
            Requires.NotNull(cache, nameof(cache));

            ImagesHttpClient = imagesHttpClient;
            Cache = cache;
        }

        private static TimeSpan CacheExpiry { get; } = TimeSpan.FromMinutes(1);

        private IImagesHttpClient ImagesHttpClient { get; }

        private IManagedCache Cache { get; }

        /// <inheritdoc/>
        public async Task<string> GetImageNameAsync(
            ImageFamilyType imageFamilyType,
            string imageFamilyName,
            string defaultImageName,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_get_image_name",
                async (childLogger) =>
                {
                    var key = GetCacheKey(PropertyName, imageFamilyType, imageFamilyName);
                    var result = await Cache.GetAsync<string>(key, logger);
                    if (result == null)
                    {
                        result = await ImagesHttpClient.GetAsync(imageFamilyType, imageFamilyName, PropertyName, defaultImageName, logger);
                        await Cache.SetAsync(key, result, CacheExpiry, logger);
                    }

                    return result;
                });
        }

        /// <inheritdoc/>
        public async Task<string> GetImageVersionAsync(
            ImageFamilyType imageFamilyType,
            string imageFamilyName,
            string defaultImageVersion,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_get_image_version",
                async (childLogger) =>
                {
                    var key = GetCacheKey(PropertyVersion, imageFamilyType, imageFamilyName);
                    var result = await Cache.GetAsync<string>(key, logger);
                    if (result == null)
                    {
                        result = await ImagesHttpClient.GetAsync(imageFamilyType, imageFamilyName, PropertyVersion, defaultImageVersion, logger);
                        await Cache.SetAsync(key, result, CacheExpiry, logger);
                    }

                    return result;
                });
        }

        private string GetCacheKey(string propertyName, ImageFamilyType imageFamilyType, string imageFamilyName)
        {
            return $"{nameof(DelegatedImageInfoProvider)}:{imageFamilyType.ToString().ToLowerInvariant()}:{imageFamilyName}:{propertyName}";
        }
    }
}
