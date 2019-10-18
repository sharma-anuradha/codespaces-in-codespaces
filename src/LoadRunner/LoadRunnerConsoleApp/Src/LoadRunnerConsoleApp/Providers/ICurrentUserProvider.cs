// <copyright file="ICurrentUserProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Providers
{
    /// <summary>
    /// Auth provider.
    /// </summary>
    public interface ICurrentUserProvider
    {
        /// <summary>
        /// Aquires access token for current user.
        /// </summary>
        /// <returns>Access token.</returns>
        Task<string> GetBearerToken();
    }
}
