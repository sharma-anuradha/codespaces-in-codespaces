// <copyright file="HeartbeatTokenResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments
{
    /// <summary>
    /// Response to a request for a heart beat token.
    /// </summary>
    public class HeartbeatTokenResult
    {
        /// <summary>
        /// Gets or sets the heartbeat token.
        /// </summary>
        public string HeartbeatToken { get; set; }
    }
}
