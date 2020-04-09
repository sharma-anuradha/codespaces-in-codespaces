// <copyright file="AuthorizationHttpContextExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <summary>
    /// Authorization-related extensions for <see cref="HttpContext" />.
    /// </summary>
    public static class AuthorizationHttpContextExtensions
    {
        /// <summary>
        /// Checks if a plan is authorized for the current HTTP context.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <param name="plan">Fully-qualified plan resource ID that the user is
        /// attempting to access, or null if the user is querying across all plans.</param>
        /// <returns>
        /// True if the plan is explicitly authorized,
        /// false if the current context is restricted to a different plan, or
        /// null if the current context is not restricted to any plan.</returns>
        public static bool? IsPlanAuthorized(this HttpContext context, string plan)
        {
            string authorizedPlan = context.GetPlan();
            return authorizedPlan == null ? (bool?)null : authorizedPlan == plan;
        }

        /// <summary>
        /// Checks if an environment is specifically authorized for the current HTTP context.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <param name="environmentId">ID of the environment the user is attempting to access,
        /// or null if the user is attempting a non-environment-scoped action.</param>
        /// <returns>
        /// True if the environment is explicitly authorized,
        /// false if the current context is restricted to different environment(s), or
        /// null if the current context is not restricted to any environment(s).</returns>
        public static bool? IsEnvironmentAuthorized(this HttpContext context, string environmentId)
        {
            string[] authorizedEnvs = context.GetEnvironments();
            return authorizedEnvs == null ? (bool?)null :
                environmentId != null && authorizedEnvs.Contains(environmentId);
        }

        /// <summary>
        /// Checks if a scope is authorized for the current HTTP context.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <param name="scope">Required scope.</param>
        /// <returns>
        /// True if the scope is explicitly authorized,
        /// false if the current context is restricted to a different scope, or
        /// null if the current context is not restricted to a scope.</returns>
        public static bool? IsScopeAuthorized(this HttpContext context, string scope)
        {
            var authorizedScopes = context.GetScopes();
            return authorizedScopes == null ? (bool?)null :
                authorizedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the current authorized plan.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <returns>Fully-qualified plan resource ID, or null if the current user does not
        /// have an explicit plan authorization.</returns>
        public static string GetPlan(this HttpContext context)
            => context.Items[HttpContextKeys.Plan] as string;

        /// <summary>
        /// Sets the current authorized plan.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <param name="plan">Fully-qualified plan resource ID, or null.</param>
        public static void SetPlan(this HttpContext context, string plan)
            => context.Items[HttpContextKeys.Plan] = plan;

        /// <summary>
        /// Gets the current authorized scopes.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <returns>Array of authorized scopes, or null if the current user does not have
        /// any specified scopes.</returns>
        public static string[] GetScopes(this HttpContext context)
            => context.Items[HttpContextKeys.Scopes] as string[];

        /// <summary>
        /// Sets the current authorized scopes.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <param name="scopes">Array of authorized scopes, or null.</param>
        public static void SetScopes(this HttpContext context, string[] scopes)
            => context.Items[HttpContextKeys.Scopes] = scopes;

        /// <summary>
        /// Gets the current authorized environments.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <returns>Array of authorized environment IDs, or null if the current user does not have
        /// any specified environments.</returns>
        public static string[] GetEnvironments(this HttpContext context)
            => context.Items[HttpContextKeys.Environments] as string[];

        /// <summary>
        /// Sets the current authorized environments.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <param name="environments">Array of authorized environment IDs, or null.</param>
        public static void SetEnvironments(this HttpContext context, string[] environments)
            => context.Items[HttpContextKeys.Environments] = environments;

        /// <summary>
        /// Keys for items stored in <see cref="HttpContext.Items" />.
        /// </summary>
        private static class HttpContextKeys
        {
            /// <summary>Plan resource ID (string?).</summary>
            public const string Plan = "VSO-Plan";

            /// <summary>Scopes claim (string[]?).</summary>
            public const string Scopes = "VSO-Scopes";

            /// <summary>Environments claim (string[]?).</summary>
            public const string Environments = "VSO-Environments";
        }
    }
}
