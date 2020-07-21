// <copyright file="IRegionalDocumentDbClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Azure.Storage.DocumentDB;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// A regional document db client provider.
    /// </summary>
    public interface IRegionalDocumentDbClientProvider : IDocumentDbClientProvider
    {
    }
}
