// <copyright file="MessageLabels.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts
{
    /// <summary>
    /// Extensions to help with messaging.
    /// </summary>
    public static class MessageLabels
    {
        /// <summary>
        /// Message label for connection-establishing queue messages.
        /// </summary>
        public static string ConnectionEstablishing { get => "connection-establishing"; }

        /// <summary>
        /// Message label for connection-established queue messages.
        /// </summary>
        public static string ConnectionEstablished { get => "connection-established"; }
    }
}
