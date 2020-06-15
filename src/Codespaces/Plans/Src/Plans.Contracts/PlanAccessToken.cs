// <copyright file="PlanAccessToken.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts
{
    /// <summary>
    /// Container for a plan access token and related metadata.
    /// </summary>
    public class PlanAccessToken
    {
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public string AccessToken { get; set; }
    }
}
