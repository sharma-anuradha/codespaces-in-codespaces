// <copyright file="PortForwardingHostUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common
{
    /// <summary>
    /// Utils for parsing and validating port forwarding hosts.
    /// </summary>
    public class PortForwardingHostUtils
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PortForwardingHostUtils"/> class.
        /// </summary>
        /// <param name="hostsConfigs">The port forwarding hosts.</param>
        public PortForwardingHostUtils(IEnumerable<HostsConfig> hostsConfigs)
        {
            var routingHostPartRegex = "(?<workspaceId>[0-9A-Fa-f]{36})-(?<port>\\d{2,5})";
            HostRegexes = hostsConfigs.SelectMany(
                hostConf => hostConf.Hosts.Select(host => string.Format(host, routingHostPartRegex)));
        }

        private IEnumerable<string> HostRegexes { get; }

        /// <summary>
        /// Parses the host and compares it with service configuration to determine if it's a valid one.
        /// </summary>
        /// <param name="hostString">The request host.</param>
        /// <returns>True if host is valid port forwarding host, false otherwise.</returns>
        public bool IsPortForwardingHost(string hostString)
        {
            return HostRegexes.Any(reg => Regex.IsMatch(hostString, reg));
        }

        /// <summary>
        /// Parses the host and extracts workspace id and port from it.
        /// </summary>
        /// <param name="hostString">The request host.</param>
        /// <param name="sessionDetails">The output session details.</param>
        /// <returns>True if workspace and port are valid.</returns>
        public bool TryGetPortForwardingSessionDetails(
            string hostString,
            out (string WorkspaceId, int Port) sessionDetails)
        {
            var currentHostRegex = HostRegexes.SingleOrDefault(reg => Regex.IsMatch(hostString, reg));
            if (currentHostRegex == default)
            {
                sessionDetails = default;
                return false;
            }

            var match = Regex.Match(hostString, currentHostRegex);
            var workspaceId = match.Groups["workspaceId"].Value;
            var portString = match.Groups["port"].Value;

            return TryGetPortForwardingSessionDetails(workspaceId, portString, out sessionDetails);
        }

        /// <summary>
        /// Parses the host and extracts workspace id and port from it.
        /// </summary>
        /// <param name="workspaceIdString">The workspace id string.</param>
        /// <param name="portString">The port string.</param>
        /// <param name="sessionDetails">The output session details.</param>
        /// <returns>True if workspace and port are valid.</returns>
        public bool TryGetPortForwardingSessionDetails(
            string workspaceIdString,
            string portString,
            out (string WorkspaceId, int Port) sessionDetails)
        {
            if (string.IsNullOrEmpty(workspaceIdString) || string.IsNullOrEmpty(portString))
            {
                sessionDetails = default;
                return false;
            }

            if (!Regex.IsMatch(workspaceIdString, "^[0-9A-Fa-f]{36}$") || !int.TryParse(portString, out var port))
            {
                sessionDetails = default;
                return false;
            }

            sessionDetails = (workspaceIdString, port);
            return true;
        }

        /// <summary>
        /// Parses the host and extracts workspace id and port from it.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="sessionDetails">The output session details.</param>
        /// <returns>True if workspace and port are valid.</returns>
        public bool TryGetPortForwardingSessionDetails(
            HttpRequest request,
            out (string WorkspaceId, int Port) sessionDetails)
        {
            sessionDetails = default;

            // 1. Headers - when set they need to be valid.
            if (request.Headers.TryGetValue(PortForwardingHeaders.WorkspaceId, out var workspaceIdValues) &&
                request.Headers.TryGetValue(PortForwardingHeaders.Port, out var portStringValues) &&
                !TryGetPortForwardingSessionDetails(
                    workspaceIdValues.SingleOrDefault(),
                    portStringValues.SingleOrDefault(),
                    out sessionDetails))
            {
                return false;
            }

            // 2. Host - in case there were no headers set to overrule it.
            if (sessionDetails == default &&
                TryGetPortForwardingSessionDetails(request.Host.ToString(), out sessionDetails))
            {
                return true;
            }

            if (sessionDetails != default)
            {
                return true;
            }

            // 3. X-Original-Url - cases where nginx is proxying the request (e.g. /auth).
            return request.Headers.TryGetValue(PortForwardingHeaders.OriginalUrl, out var originalUrlValues) &&
                   Uri.TryCreate(originalUrlValues.SingleOrDefault(), UriKind.Absolute, out var originalUrl) &&
                   TryGetPortForwardingSessionDetails(originalUrl.Host, out sessionDetails);
        }
    }
}