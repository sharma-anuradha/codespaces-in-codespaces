// <copyright file="PlanAccessTokenScopes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts
{
    /// <summary>
    /// Provides constants for scopes related to Plan access tokens.
    /// </summary>
    public class PlanAccessTokenScopes
    {
        /// <summary>
        /// A scope allowing the user to read all environments in a plan.
        /// </summary>
        public const string ReadEnvironments = "read:allenvironments";

        /// <summary>
        /// A scope allowing the user to perform CRUD operations on and connect to environments they own in a plan.
        /// </summary>
        public const string WriteEnvironments = "write:environments";

        /// <summary>
        /// A scope allowing the user to delete all environments in a plan.
        /// </summary>
        public const string DeleteEnvironments = "delete:allenvironments";

        /// <summary>
        /// The set of all valid plan access token scopes.
        /// </summary>
        public static readonly string[] ValidScopes = new[]
        {
            ReadEnvironments,
            WriteEnvironments,
            DeleteEnvironments,
        };
    }
}
