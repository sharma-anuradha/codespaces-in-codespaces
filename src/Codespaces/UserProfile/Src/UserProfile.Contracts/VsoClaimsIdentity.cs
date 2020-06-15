// <copyright file="VsoClaimsIdentity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// Vso Claims Identity.
    /// </summary>
    public class VsoClaimsIdentity : ClaimsIdentity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VsoClaimsIdentity"/> class.
        /// </summary>
        /// <param name="claimsIdentity">Target claims identity.</param>
        public VsoClaimsIdentity(
            ClaimsIdentity claimsIdentity)
            : base(claimsIdentity)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VsoClaimsIdentity"/> class.
        /// </summary>
        /// <param name="authorizedPlan">Target authorized plan.</param>
        /// <param name="scopes">Target scope.</param>
        /// <param name="authorizedEnvironments">Target authorized environments.</param>
        /// <param name="claimsIdentity">Target claims identity.</param>
        public VsoClaimsIdentity(
            string authorizedPlan,
            string[] scopes,
            string[] authorizedEnvironments,
            ClaimsIdentity claimsIdentity)
            : base(claimsIdentity)
        {
            AuthorizedPlan = authorizedPlan;
            Scopes = scopes;
            AuthorizedEnvironments = authorizedEnvironments;
        }

        /// <summary>
        /// Gets the current authorized plan.
        /// </summary>
        /// <returns>Fully-qualified plan resource ID, or null if the current user does not
        /// have an explicit plan authorization.</returns>
        public string AuthorizedPlan { get; private set; }

        /// <summary>
        /// Gets the current authorized scopes.
        /// </summary>
        /// <returns>Array of authorized scopes, or null if the current user does not have
        /// any specified scopes.</returns>
        public string[] Scopes { get; private set; }

        /// <summary>
        /// Gets the current authorized environments.
        /// </summary>
        /// <returns>Array of authorized environment IDs, or null if the current user does not have
        /// any specified environments.</returns>
        public string[] AuthorizedEnvironments { get; private set; }

        /// <summary>
        /// Checks if a plan is authorized for the current HTTP context.
        /// </summary>
        /// <param name="plan">Fully-qualified plan resource ID that the user is
        /// attempting to access, or null if the user is querying across all plans.</param>
        /// <returns>
        /// True if the plan is explicitly authorized,
        /// false if the current context is restricted to a different plan, or
        /// null if the current context is not restricted to any plan.</returns>
        public virtual bool? IsPlanAuthorized(string plan)
        {
            string authorizedPlan = AuthorizedPlan;
            return authorizedPlan == null ? (bool?)null : authorizedPlan == plan;
        }

        /// <summary>
        /// Checks if an environment is specifically authorized for the current HTTP context.
        /// </summary>
        /// <param name="environmentId">ID of the environment the user is attempting to access,
        /// or null if the user is attempting a non-environment-scoped action.</param>
        /// <returns>
        /// True if the environment is explicitly authorized,
        /// false if the current context is restricted to different environment(s), or
        /// null if the current context is not restricted to any environment(s).</returns>
        public virtual bool? IsEnvironmentAuthorized(string environmentId)
        {
            string[] authorizedEnvs = AuthorizedEnvironments;
            return authorizedEnvs == null ? (bool?)null :
                environmentId != null && authorizedEnvs.Contains(environmentId);
        }

        /// <summary>
        /// Checks to see if the user is anonymous.
        /// </summary>
        /// <returns>False if user is not anonymous, true if they are.</returns>
        public virtual bool IsAnonymous()
        {
            return false;
        }

        /// <summary>
        /// Checks if a scope is authorized for the current HTTP context.
        /// </summary>
        /// <param name="scope">Required scope.</param>
        /// <returns>
        /// True if the scope is explicitly authorized,
        /// false if the current context is restricted to a different scope, or
        /// null if the current context is not restricted to a scope.</returns>
        public virtual bool? IsScopeAuthorized(string scope)
        {
            var authorizedScopes = Scopes;
            return authorizedScopes == null ? (bool?)null :
                authorizedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
        }

        private void Initialize()
        {
            // Extract plan resource id
            AuthorizedPlan = FindFirst(CustomClaims.PlanResourceId)?.Value;

            // If the plan claim is not set, then we want to ignore any incoming scopes as the token was not generated by us
            if (!string.IsNullOrEmpty(AuthorizedPlan))
            {
                // Extract scopes
                const string altScopeClaimName = "http://schemas.microsoft.com/identity/claims/scope";
                var claimsScope = FindFirst(CustomClaims.Scope)?.Value ?? FindFirst(altScopeClaimName)?.Value;
                Scopes = claimsScope == null ? null : claimsScope.Length == 0 ? Array.Empty<string>() : claimsScope.Split(' ');
                if (Scopes != null && Scopes.Contains("all", StringComparer.OrdinalIgnoreCase))
                {
                    // Having an "all" scope is equivalent to an unscoped token.
                    Scopes = null;
                }

                // Extract environment id
                AuthorizedEnvironments = Claims.Where((c) => c.Type == CustomClaims.Environments).Select((c) => c.Value).ToArray();
                if (AuthorizedEnvironments.Length == 0)
                {
                    AuthorizedEnvironments = null;
                }
            }
        }
    }
}
