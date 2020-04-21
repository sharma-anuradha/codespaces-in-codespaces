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
        private const string EnvironmentIdRegex = "[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}";
        private const string WorkspaceIdRegex = "[0-9A-Fa-f]{36}";
        private readonly string routingHostPartRegexAllowEnvironments = $"(?:(?<environmentId>{EnvironmentIdRegex})|(?<workspaceId>{WorkspaceIdRegex}))-(?<port>\\d{"{2,5}"})";
        private readonly string routingHostPartRegex = $"(?:(?<workspaceId>{WorkspaceIdRegex}))-(?<port>\\d{"{2,5}"})";

        /// <summary>
        /// Initializes a new instance of the <see cref="PortForwardingHostUtils"/> class.
        /// </summary>
        /// <param name="hostsConfigs">The port forwarding hosts.</param>
        public PortForwardingHostUtils(IEnumerable<HostsConfig> hostsConfigs)
        {
            HostRegexes = hostsConfigs.SelectMany(
                hostConf => hostConf.Hosts.Select(host => string.Format(host, hostConf.AllowEnvironmentIdBasedHosts ? routingHostPartRegexAllowEnvironments : routingHostPartRegex)));
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
            out (string WorkspaceId, string EnvironmentId, int Port) sessionDetails)
        {
            var currentHostRegex = HostRegexes.SingleOrDefault(reg => Regex.IsMatch(hostString, reg));
            if (currentHostRegex == default)
            {
                sessionDetails = default;
                return false;
            }

            var match = Regex.Match(hostString, currentHostRegex);

            var id = string.Empty;
            if (match.Groups["workspaceId"].Success)
            {
                id = match.Groups["workspaceId"].Value;
            }
            else if (match.Groups["environmentId"].Success)
            {
                id = match.Groups["environmentId"].Value;
            }
            else
            {
                sessionDetails = default;
                return false;
            }

            var portString = match.Groups["port"].Value;

            return TryGetPortForwardingSessionDetails(id, portString, out sessionDetails);
        }

        /// <summary>
        /// Parses the host and extracts workspace id and port from it.
        /// </summary>
        /// <param name="idString">The workspace or environment id string.</param>
        /// <param name="portString">The port string.</param>
        /// <param name="sessionDetails">The output session details.</param>
        /// <returns>True if workspace and port are valid.</returns>
        public bool TryGetPortForwardingSessionDetails(
            string idString,
            string portString,
            out (string WorkspaceId, string EnvironmentId, int Port) sessionDetails)
        {
            if (string.IsNullOrEmpty(idString) || string.IsNullOrEmpty(portString))
            {
                sessionDetails = default;
                return false;
            }

            if (!int.TryParse(portString, out var port))
            {
                sessionDetails = default;
                return false;
            }

            if (Regex.IsMatch(idString, $"^{WorkspaceIdRegex}$"))
            {
                sessionDetails = (idString, default, port);
                return true;
            }

            if (Regex.IsMatch(idString, $"^{EnvironmentIdRegex}$"))
            {
                sessionDetails = (default, idString, port);
                return true;
            }

            sessionDetails = default;
            return false;
        }

        /// <summary>
        /// Parses the host and extracts workspace id and port from it.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="sessionDetails">The output session details.</param>
        /// <returns>True if workspace and port are valid.</returns>
        public bool TryGetPortForwardingSessionDetails(
            HttpRequest request,
            out (string WorkspaceId, string EnvironmentId, int Port) sessionDetails)
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