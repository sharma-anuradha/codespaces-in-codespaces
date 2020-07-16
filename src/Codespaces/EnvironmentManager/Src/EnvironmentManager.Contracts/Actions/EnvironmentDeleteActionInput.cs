// <copyright file="EnvironmentDeleteActionInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts.Actions
{
    /// <summary>
    /// Environment Delete Action Input.
    /// </summary>
    public class EnvironmentDeleteActionInput : IEntityActionIdInput
    {
        /// <summary>
        ///  Gets or sets the cloud environment record to be deleted.
        /// </summary>
        public CloudEnvironment CloudEnvironment { get; set; }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse(CloudEnvironment.Id);
    }
}
