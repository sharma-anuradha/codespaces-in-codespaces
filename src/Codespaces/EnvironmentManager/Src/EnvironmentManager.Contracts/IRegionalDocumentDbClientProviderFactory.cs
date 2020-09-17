// <copyright file="IRegionalDocumentDbClientProviderFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// A factory used to create <see cref="IDocumentDbClientProvider"/> instances.
    /// </summary>
    public interface IRegionalDocumentDbClientProviderFactory
    {
        /// <summary>
        /// Gets the regional document database client provider.
        /// </summary>
        /// <param name="controlPlaneLocation">The control-plane location.</param>
        /// <returns>The regional document database client provider.</returns>
        IRegionalDocumentDbClientProvider GetRegionalClientProvider(AzureLocation controlPlaneLocation);
    }
}
