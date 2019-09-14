// <copyright file="IVirtualMachineTokenValidator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Provides methods for authentication.
    /// </summary>
    public interface IVirtualMachineTokenValidator
    {
        /// <summary>
        /// Verifies the token.
        /// </summary>
        /// <param name="token">Token.</param>
        /// <param name="logger">logger.</param>
        /// <returns>JwtPayload if successful, null otherwise.</returns>
        Task<TokenValidationParameters> GetTokenValidationParameters();

        /// <summary>
        /// Gets the jwt payload from the token.
        /// </summary>
        /// <param name="token">Token.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<JwtPayload> GetPayload(string token);
    }
}
