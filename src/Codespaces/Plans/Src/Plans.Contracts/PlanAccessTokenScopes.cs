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
        /// <remarks>
        /// Equivalent to <see cref="ReadCodespaces" />, but supporting old terminology.
        /// </remarks>
        public const string ReadEnvironments = "read:allenvironments";

        /// <summary>
        /// A scope allowing the user to perform CRUD operations on and connect to environments they own in a plan.
        /// </summary>
        /// <remarks>
        /// Equivalent to <see cref="WriteCodespaces" />, but supporting old terminology.
        /// </remarks>
        public const string WriteEnvironments = "write:environments";

        /// <summary>
        /// A scope allowing the user to delete all environments in a plan.
        /// </summary>
        /// <remarks>
        /// Equivalent to <see cref="DeleteCodespaces" />, but supporting old terminology.
        /// </remarks>
        public const string DeleteEnvironments = "delete:allenvironments";

        /// <summary>
        /// A scope allowing the user to read all codespaces in a plan.
        /// </summary>
        public const string ReadCodespaces = "read:allcodespaces";

        /// <summary>
        /// A scope allowing the user to perform CRUD operations on and connect to codespaces they own in a plan.
        /// </summary>
        public const string WriteCodespaces = "write:codespaces";

        /// <summary>
        /// A scope allowing the user to delete all codespaces in a plan.
        /// </summary>
        public const string DeleteCodespaces = "delete:allcodespaces";

        /// <summary>
        /// A scope allowing the user to share a Live Share session.
        /// </summary>
        public const string ShareSession = "share:session";

        /// <summary>
        /// A scope allowing the user to join a Live Share session.
        /// </summary>
        public const string JoinSession = "join:session";

        /// <summary>
        /// The set of all valid plan access token scopes.
        /// </summary>
        public static readonly string[] ValidPlanScopes = new[]
        {
            ReadEnvironments,
            WriteEnvironments,
            DeleteEnvironments,
            ReadCodespaces,
            WriteCodespaces,
            DeleteCodespaces,
        };

        /// <summary>
        /// The set of all valid Live Share session access token scopes.
        /// </summary>
        public static readonly string[] ValidSessionScopes = new[]
        {
            ShareSession,
            JoinSession,
        };
    }
}
