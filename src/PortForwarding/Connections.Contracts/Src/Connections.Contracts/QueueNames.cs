// <copyright file="QueueNames.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts
{
    /// <summary>
    /// Constant queue names.
    /// </summary>
    public static class QueueNames
    {
        /// <summary>
        /// Gets queue name for "connections-new" queue.
        /// </summary>
        public static string NewConnections { get => "connections-new"; }

        /// <summary>
        /// Gets queue name for "connections-established" queue.
        /// </summary>
        public static string EstablishedConnections { get => "connections-established"; }
    }
}
