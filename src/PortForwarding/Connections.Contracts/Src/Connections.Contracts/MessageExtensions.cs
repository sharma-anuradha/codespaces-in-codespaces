// <copyright file="MessageExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts
{
    /// <summary>
    /// Extensions to help with messaging.
    /// </summary>
    public static class MessageExtensions
    {
        /// <summary>
        /// Gets messaging session id for established connection.
        /// </summary>
        /// <param name="connection">Connection details message.</param>
        /// <returns>Session id to listen to on service bus queue.</returns>
        public static string GetMessagingSessionId(this ConnectionEstablished connection)
        {
            return $"{connection.WorkspaceId.ToLower()}-{connection.SourcePort}";
        }
    }
}
