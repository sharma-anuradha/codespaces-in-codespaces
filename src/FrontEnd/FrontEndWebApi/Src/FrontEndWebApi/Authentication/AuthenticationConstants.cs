// <copyright file="AuthenticationConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <summary>
    /// Front-end authentication constants, shared between JWT Bearer and Cookie authentication.
    /// </summary>
    public static class AuthenticationConstants
    {
        /// <summary>
        /// Allow alternate audiences for compatibility. If this is false, only the first-party appid audiecne is allowed.
        /// </summary>
        public static readonly bool UseCompatibilityAudiences = true;

        /// <summary>
        /// Specifies whether the token requires a valid email claim.
        /// </summary>
        public static readonly bool IsEmailClaimRequired = true;
    }
}
