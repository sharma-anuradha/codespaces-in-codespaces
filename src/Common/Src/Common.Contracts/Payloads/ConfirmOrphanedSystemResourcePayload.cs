// <copyright file="ConfirmOrphanedSystemResourcePayload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts.Payloads
{
    /// <summary>
    /// Request payload from the Backend to the Frontend to confirm a System Resource is orphaned.
    /// </summary>
    /// <typeparam name="T">The response payload type.</typeparam>
    public class ConfirmOrphanedSystemResourcePayload : JobPayload
    {
        /// <summary>
        /// The system resource id to confirm orphan status of.
        /// </summary>
        public string ResourceId { get; set; }

        /// <summary>
        /// The resource type of the potential orphan.
        /// </summary>
        public ResourceType ResourceType { get; set; }
    }
}
