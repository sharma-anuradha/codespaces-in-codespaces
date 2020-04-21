// <copyright file="PortForwardingRoutingHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Routing
{
    /// <summary>
    /// Common implementation for determining if current request is a port forwarding request based on either headers
    /// or hostname.
    /// </summary>
    public class PortForwardingRoutingHelper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PortForwardingRoutingHelper"/> class.
        /// </summary>
        /// <param name="hostUtils">The portforwarding host utils.</param>
        public PortForwardingRoutingHelper(PortForwardingHostUtils hostUtils)
        {
            HostUtils = hostUtils;
        }

        private PortForwardingHostUtils HostUtils { get; }

        /// <summary>
        /// Checks current httpContext host and headers for required port forwarding attributes.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>True when the request is made for port forwarding, false otherwise.</returns>
        public bool IsPortForwardingRequest(HttpContext context)
        {
            var logger = context.GetLogger();
            if (HostUtils.TryGetPortForwardingSessionDetails(context.Request, out var sessionDetails))
            {
                logger?.AddBaseValue("routing_context", "PortForwarding");
                logger?.AddBaseValue("port", sessionDetails.Port.ToString());

                switch (sessionDetails)
                {
                    case EnvironmentSessionDetails details:
                        logger?.AddBaseValue("workspace_id", details.WorkspaceId);
                        logger?.AddBaseValue("environment_id", details.EnvironmentId);
                        break;
                    case PartialEnvironmentSessionDetails details:
                        logger?.AddBaseValue("environment_id", details.EnvironmentId);
                        break;
                    case WorkspaceSessionDetails details:
                        logger?.AddBaseValue("workspace_id", details.WorkspaceId);
                        break;
                }

                return true;
            }

            logger?.AddBaseValue("routing_context", "Internal");

            return false;
        }
    }
}