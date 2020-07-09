// <copyright file="TopicNames.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts
{
    /// <summary>
    /// Constant topic names.
    /// </summary>
    public static class TopicNames
    {
        /// <summary>
        /// Gets queue name for "connections-new" queue.
        /// </summary>
        public static string Errors => "connection-errors";
    }
}
