// <copyright file="ICloudEnvironmentTokenProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions
{
    /// <summary>
    /// Provides methods to create security tokens.
    /// </summary>
    public interface IVSSaaSTokenProvider
    {
        /// <summary>
        /// Generates a token.
        /// </summary>
        /// <param name="identifier">Id of the resource.</param>
        /// <returns>security token.</returns>
        string Generate(string identifier);
    }
}