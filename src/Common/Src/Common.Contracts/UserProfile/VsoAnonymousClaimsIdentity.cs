// <copyright file="VsoAnonymousClaimsIdentity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Security.Claims;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Vso Anonymous Claims Identity.
    /// </summary>
    public class VsoAnonymousClaimsIdentity : VsoClaimsIdentity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VsoAnonymousClaimsIdentity"/> class.
        /// </summary>
        /// <param name="claimsIdentity">Target claims identity.</param>
        public VsoAnonymousClaimsIdentity(
            ClaimsIdentity claimsIdentity)
            : base(claimsIdentity)
        {
        }

        /// <inheritdoc/>
        public override bool? IsPlanAuthorized(string plan)
        {
            return false;
        }

        /// <inheritdoc/>
        public override bool? IsEnvironmentAuthorized(string environmentId)
        {
            return false;
        }

        /// <inheritdoc/>
        public override bool? IsAnyScopeAuthorized(params string[] scopes)
        {
            return false;
        }

        /// <inheritdoc/>
        public override bool IsAnonymous()
        {
            return true;
        }
    }
}
