// <copyright file="IdentityContext.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// Identity context.
    /// </summary>
    public class IdentityContext
    {
        /// <summary>
        /// Gets or sets vso claims identity.
        /// </summary>
        public VsoClaimsIdentity Identity { get; set; }

        /// <summary>
        /// Gets or sets the user ID set.
        /// </summary>
        public UserIdSet UserIdSet { get; set; }
    }
}
