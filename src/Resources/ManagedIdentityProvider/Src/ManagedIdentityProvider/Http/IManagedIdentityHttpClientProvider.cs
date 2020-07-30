// <copyright file="IManagedIdentityHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ManagedIdentityProvider
{
    /// <summary>
    /// Provides a singleton instance of a Azure Managed Identity HTTP Client.
    /// </summary>
    public interface IManagedIdentityHttpClientProvider : IHttpClientProvider
    {
    }
}
