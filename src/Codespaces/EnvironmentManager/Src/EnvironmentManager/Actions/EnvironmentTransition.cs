// <copyright file="EnvironmentTransition.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Entity Transition.
    /// </summary>
    public class EnvironmentTransition : BaseEntityTransition<CloudEnvironment>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentTransition"/> class.
        /// </summary>
        /// <param name="environment">Target environment.</param>
        public EnvironmentTransition(CloudEnvironment environment)
            : base(environment)
        {
            OriginalState = environment.State;
        }

        private CloudEnvironmentState OriginalState { get; }

        /// <inheritdoc/>
        public override bool IsValid(CloudEnvironment persistedEntity)
        {
            return OriginalState == persistedEntity.State;
        }
    }
}
