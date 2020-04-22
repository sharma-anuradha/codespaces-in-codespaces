// <copyright file="ProviderNames.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.TokenService.Contracts
{
    /// <summary>
    /// Names identity providers known to the token service, for which token-exchange is supported.
    /// </summary>
    public static class ProviderNames
    {
        /// <summary>
        /// Microsoft (AAD/MSA) identity provider.
        /// </summary>
        public const string Microsoft = "microsoft";

        /// <summary>
        /// GitHub identity provider.
        /// </summary>
        public const string GitHub = "github";
    }
}
