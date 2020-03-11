// <copyright file="MessageKubernetesObjectExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using k8s.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings
{
    /// <summary>
    /// Extensions to help translate PFS DTOs to Kubernetes objects.
    /// </summary>
    public static class MessageKubernetesObjectExtensions
    {
        /// <summary>
        /// Converts the <see cref="ConnectionEstablishing"/> agent to the Kubernetes <see cref="V1OwnerReference"/>.
        /// </summary>
        /// <param name="connection">The connection reference for agent we'll be setting as the owner of service and ingress rule.</param>
        /// <returns>The owner object.</returns>
        public static V1OwnerReference ToOwnerReference(this ConnectionDetails connection)
        {
            return new V1OwnerReference
            {
                ApiVersion = "v1",
                Kind = "Pod",
                Name = connection.AgentName,
                Uid = connection.AgentUid,
            };
        }

        /// <summary>
        /// Returns Kubernetes service name based on connection mapping.
        /// </summary>
        /// <param name="connection">Connection mapping.</param>
        /// <returns>Kubernetes service name.</returns>
        public static string GetKubernetesServiceName(this ConnectionDetails connection)
        {
            return $"{connection.WorkspaceId.ToLower()}-{connection.SourcePort}";
        }
    }
}