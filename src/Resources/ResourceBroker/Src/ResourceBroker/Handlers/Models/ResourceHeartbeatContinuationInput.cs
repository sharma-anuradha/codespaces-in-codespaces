// <copyright file="ResourceHeartbeatContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Resource provider create input.
    /// </summary>
    public class ResourceHeartbeatContinuationInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets resource id.
        /// </summary>
        public Guid ResourceId { get; set; }

        /// <summary>
        /// Gets or sets heartbeat data.
        /// </summary>
        public HeartBeatInput HeartBeatData { get; set; }
    }
}