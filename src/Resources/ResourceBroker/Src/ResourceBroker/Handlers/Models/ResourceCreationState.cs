// <copyright file="ResourceCreationState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Resource creation states.
    /// </summary>
    public enum ResourceCreationState
    {
        /// <summary>
        /// Create components.
        /// </summary>
        CreateComponent = 0,

        /// <summary>
        /// Create Resource.
        /// </summary>
        CreateResource = 1,
    }
}