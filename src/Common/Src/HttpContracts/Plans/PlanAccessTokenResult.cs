// <copyright file="PlanAccessTokenResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Plans
{
    /// <summary>
    /// The plan access token API result.
    /// </summary>
    public class PlanAccessTokenResult
    {
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public string AccessToken { get; set; }
    }
}
