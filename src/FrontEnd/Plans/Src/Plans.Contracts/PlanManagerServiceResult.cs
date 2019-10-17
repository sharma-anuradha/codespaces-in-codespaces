// <copyright file="PlanManagerServiceResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts
{
    /// <summary>
    /// Represents the result returned from the AccountManager.
    /// </summary>
    public struct PlanManagerServiceResult
    {
        /// <summary>
        /// Gets or sets the VsoPlan object.
        /// </summary>
        public VsoPlan VsoPlan { get; set; }

        /// <summary>
        /// Gets or sets the error code for the user/client.
        /// </summary>
        public ErrorCodes ErrorCode { get; set; }
    }
}
