// <copyright file="ITokenProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Tokens;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Wrapper interface to encompass a <see cref="IJwtWriter"/> for creating tokens from global <see cref="AuthenticationSettings"/>.
    /// </summary>
    /// <remarks>
    /// As this is a wrapper only, usage of should be primarily driven by extension methods.
    /// </remarks>
    public interface ITokenProvider
    {
        /// <summary>
        /// Gets the Jwt writer.
        /// </summary>
        IJwtWriter JwtWriter { get; }

        /// <summary>
        /// Gets the authentication settings.
        /// </summary>
        AuthenticationSettings Settings { get; }
    }
}