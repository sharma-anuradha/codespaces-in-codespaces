// <copyright file="EnvironmentGetAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Get Action.
    /// </summary>
    public class EnvironmentGetAction : EnvironmentItemAction<Guid>, IEnvironmentGetAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentGetAction"/> class.
        /// </summary>
        /// <param name="environmentStateManager">Target environment state manager.</param>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="systemActionGetProvider">Target system action get provider.</param>
        /// <param name="environmentAccessManager">Target access authorization manager.</param>
        public EnvironmentGetAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager)
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager)
        {
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_get_action";

        /// <inheritdoc/>
        protected override async Task<CloudEnvironment> RunCoreAsync(Guid input, IDiagnosticsLogger logger)
        {
            // Fetch Record
            var record = await FetchAsync(input, logger.NewChildLogger());

            if (record == null)
            {
                throw new EntityNotFoundException($"Target '{input}' not found.");
            }

            return record.Value;
        }
    }
}
