// <copyright file="IEnvironmentAccessManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment access manager.
    /// </summary>
    public interface IEnvironmentAccessManager
    {
        /// <summary>
        /// Checks if the current user is authorized to access an environment.
        /// </summary>
        /// <param name="environment">Environment that the user is attempting to access.</param>
        /// <param name="nonOwnerScopes">The user is required to have at least one of these scopes
        /// If they do not have full owner-level access to the environment. Or null if only
        /// owners should be authorized.</param>
        /// <param name="logger">Diagnostic logger.</param>
        void AuthorizeEnvironmentAccess(CloudEnvironment environment, string[] nonOwnerScopes, IDiagnosticsLogger logger);

        /// <summary>
        /// Checks if the current user is authorized to access a plan.
        /// </summary>
        /// <param name="plan">Plan that the user is attempting to access.</param>
        /// <param name="requiredScopes">A user must have at least one of these scopes,
        /// if they have a scoped access token.</param>
        /// <param name="identity">The identity to check, or null to use the current user identity.</param>
        /// <param name="logger">Diagnostic logger.</param>
        void AuthorizePlanAccess(VsoPlan plan, string[] requiredScopes, VsoClaimsIdentity identity, IDiagnosticsLogger logger);
    }
}