// <copyright file="ListEnvironmentActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Environment List Action Input.
    /// </summary>
    public class ListEnvironmentActionInput
    {
        /// <summary>
        /// Gets or sets name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets plan id.
        /// </summary>
        public string PlanId { get; set; }

        /// <summary>
        /// Gets or sets the location of the environments if known.
        /// </summary>
        public AzureLocation? Location { get; set; }

        /// <summary>
        /// Gets or sets the user id set.
        /// </summary>
        public UserIdSet UserIdSet { get; set; }

        /// <summary>
        /// Gets or sets the environment list type.
        /// </summary>
        public EnvironmentListType EnvironmentListType { get; set; }
    }
}
