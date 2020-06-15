// <copyright file="MockImagesHttpClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.Images
{
    /// <inheritdoc/>
    public class MockImagesHttpClient : IImagesHttpClient
    {
        /// <inheritdoc/>
        public Task<string> GetAsync(
            ImageFamilyType imageType,
            string family,
            string property,
            string defaultValue,
            IDiagnosticsLogger logger)
        {
            return Task.FromResult(defaultValue);
        }
    }
}
