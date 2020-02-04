// <copyright file="CustomClaims.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Provides constants for custom claim names used by tokens in the service.
    /// </summary>
    public class CustomClaims
    {
        /// <summary>
        /// The idp claim.
        /// </summary>
        public const string Provider = "idp";

        /// <summary>
        /// The tid claim.
        /// </summary>
        public const string TenantId = "tid";

        /// <summary>
        /// The oid claim.
        /// </summary>
        public const string OId = "oid";

        /// <summary>
        /// The user's username.
        /// </summary>
        public const string Username = "preferred_username";

        /// <summary>
        /// The user's unique name.
        /// </summary>
        public const string UniqueName = "unique_name";

        /// <summary>
        /// The user's display name.
        /// </summary>
        public const string DisplayName = "name";

        /// <summary>
        /// The user's email address.
        /// </summary>
        public const string Email = "email";

        /// <summary>
        /// The full ResourceId of a plan.
        /// </summary>
        public const string PlanResourceId = "plan";

        /// <summary>
        /// The scope of the token.
        /// </summary>
        public const string Scope = "scp";
    }
}
