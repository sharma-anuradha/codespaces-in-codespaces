// <copyright file="VsoSuperuserClaimsIdentity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Security.Claims;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Vso super user claims identity, that has full sope authorization to all plans and environments.
    /// </summary>
    public sealed class VsoSuperuserClaimsIdentity : VsoClaimsIdentity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VsoSuperuserClaimsIdentity"/> class.
        /// </summary>
        /// <param name="scopes">Target scope.</param>
        /// <param name="authorizedEnvironments">Target authorized environments.</param>
        /// <param name="claimsIdentity">Target claims identity.</param>
        public VsoSuperuserClaimsIdentity(
            string[] scopes,
            ClaimsIdentity claimsIdentity)
            : base(scopes, claimsIdentity)
        {
        }

        /// <inheritdoc/>
        public override bool IsSuperuser()
        {
            return true;
        }
    }
}
