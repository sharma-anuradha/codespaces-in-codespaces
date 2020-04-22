// <copyright file="AuthenticationHttpContextExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Services.TokenService.Settings;

namespace Microsoft.VsSaaS.Services.TokenService.Authentication
{
    /// <summary>
    /// Extension methods related to token service authentication.
    /// </summary>
    public static class AuthenticationHttpContextExtensions
    {
        private const string ClientSettingsKey = "TokenClientSettings";

        /// <summary>
        /// Gets the client settings for the current request.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <returns>Settings for the current client, or null if the current client
        /// is not a known client.</returns>
        public static TokenClientSettings? GetClientSettings(this HttpContext context)
            => context.Items[ClientSettingsKey] as TokenClientSettings;

        /// <summary>
        /// Sets the client settings for the current request.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <param name="clientSettings">Settings for the current client.</param>
        public static void SetClientSettings(
            this HttpContext context, TokenClientSettings? clientSettings)
            => context.Items[ClientSettingsKey] = clientSettings;
    }
}
