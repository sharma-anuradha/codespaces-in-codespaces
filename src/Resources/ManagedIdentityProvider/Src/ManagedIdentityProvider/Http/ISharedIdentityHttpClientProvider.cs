// <copyright file="ISharedIdentityHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ManagedIdentityProvider
{
    /// <summary>
    /// Provides a singleton instance of a Shared Identities HTTP Client.
    /// </summary>
    public interface ISharedIdentityHttpClientProvider : IHttpClientProvider
    {
    }
}
