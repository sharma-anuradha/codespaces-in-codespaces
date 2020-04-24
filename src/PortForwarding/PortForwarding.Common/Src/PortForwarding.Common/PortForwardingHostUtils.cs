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

        /// <summary>
        /// Initializes a new instance of the <see cref="PortForwardingHostUtils"/> class.
        /// </summary>
        /// <param name="hostsConfigs">The port forwarding hosts.</param>
        public PortForwardingHostUtils(IEnumerable<HostsConfig> hostsConfigs)
        {
            HostRegexes = hostsConfigs.SelectMany(
                hostConf => hostConf.Hosts.Select(host => string.Format(host, routingHostPartRegexAllowEnvironments)));
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
            out PortForwardingSessionDetails sessionDetails)
        {
            if (string.IsNullOrEmpty(hostString))
            {
                sessionDetails = default;
                return false;
            }

            var currentHostRegex = HostRegexes.SingleOrDefault(reg => Regex.IsMatch(hostString, reg));
            if (currentHostRegex == default)
            {
                sessionDetails = default;
                return false;
            }

            var match = Regex.Match(hostString, currentHostRegex);

            var workspaceId = match.Groups["workspaceId"].Value;
            var environmentId = match.Groups["environmentId"].Value;
            var portString = match.Groups["port"].Value;

            return TryGetPortForwardingSessionDetails(workspaceId, environmentId, portString, out sessionDetails);
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
            out PortForwardingSessionDetails sessionDetails)
        {
            return TryGetPortForwardingSessionDetails(workspaceIdString, null, portString, out sessionDetails);
        }

        /// <summary>
        /// Parses the host and extracts workspace id and port from it.
        /// </summary>
        /// <param name="workspaceIdString">The workspace id string.</param>
        /// <param name="environmentIdString">The environment id string.</param>
        /// <param name="portString">The port string.</param>
        /// <param name="sessionDetails">The output session details.</param>
        /// <returns>True if workspace and port are valid.</returns>
        public bool TryGetPortForwardingSessionDetails(
        string workspaceIdString,
        string environmentIdString,
        string portString,
        out PortForwardingSessionDetails sessionDetails)
        {
            if (string.IsNullOrEmpty(portString))
            {
                sessionDetails = default;
                return false;
            }

            if (string.IsNullOrEmpty(workspaceIdString) && string.IsNullOrEmpty(environmentIdString))
            {
                sessionDetails = default;
                return false;
            }

            if (!int.TryParse(portString, out var port))
            {
                sessionDetails = default;
                return false;
            }

            string workspaceId = default;
            if (!string.IsNullOrEmpty(workspaceIdString) && Regex.IsMatch(workspaceIdString, $"^{WorkspaceIdRegex}$"))
            {
                workspaceId = workspaceIdString;
            }

            string environmentId = default;
            if (!string.IsNullOrEmpty(environmentIdString) && Regex.IsMatch(environmentIdString, $"^{EnvironmentIdRegex}$"))
            {
                environmentId = environmentIdString;
            }

            sessionDetails = (workspaceId, environmentId) switch
            {
                (string w, string e) => new EnvironmentSessionDetails(w, e, port),
                (null, string e) => new PartialEnvironmentSessionDetails(e, port),
                (string w, null) => new WorkspaceSessionDetails(w, port),
                _ => default,
            };

            return sessionDetails != default;
        }

        /// <summary>
        /// Parses the host and extracts workspace id and port from it.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="sessionDetails">The output session details.</param>
        /// <returns>True if workspace and port are valid.</returns>
        public bool TryGetPortForwardingSessionDetails(
            HttpRequest request,
            out PortForwardingSessionDetails sessionDetails)
        {
            sessionDetails = default;

            string workspaceIdHeaderValue = default;
            if (request.Headers.TryGetValue(PortForwardingHeaders.WorkspaceId, out var tempWorkspaceIdValues))
            {
                workspaceIdHeaderValue = tempWorkspaceIdValues.SingleOrDefault();
            }

            string environmentIdHeaderValue = default;
            if (request.Headers.TryGetValue(PortForwardingHeaders.EnvironmentId, out var tempEnvironmentIdValues))
            {
                environmentIdHeaderValue = tempEnvironmentIdValues.SingleOrDefault();
            }

            string portHeaderValue = default;
            if (request.Headers.TryGetValue(PortForwardingHeaders.Port, out var tempPortStringValues))
            {
                portHeaderValue = tempPortStringValues.SingleOrDefault();
            }

            string originalUriHost = default;
            if (request.Headers.TryGetValue(PortForwardingHeaders.OriginalUrl, out var originalUrlValues) &&
                Uri.TryCreate(originalUrlValues.SingleOrDefault(), UriKind.Absolute, out var originalUrl))
            {
                originalUriHost = originalUrl.Host.ToString();
            }

            TryGetPortForwardingSessionDetails(workspaceIdHeaderValue, environmentIdHeaderValue, portHeaderValue, out var headerSessionDetails);
            TryGetPortForwardingSessionDetails(request.Host.ToString(), out var hostSessionDetails);
            TryGetPortForwardingSessionDetails(originalUriHost, out var originalUrlSessionDetails);

            sessionDetails = (headerSessionDetails, hostSessionDetails, originalUrlSessionDetails) switch
            {
                (EnvironmentSessionDetails headers, PartialEnvironmentSessionDetails host, null) when string.Equals(headers.EnvironmentId, host.EnvironmentId) => headers,
                (EnvironmentSessionDetails headers, null, PartialEnvironmentSessionDetails host) when string.Equals(headers.EnvironmentId, host.EnvironmentId) => headers,
                (EnvironmentSessionDetails headers, null, null) => headers,
                (null, PartialEnvironmentSessionDetails host, null) => host,
                (null, null, PartialEnvironmentSessionDetails host) => host,
                (WorkspaceSessionDetails headers, WorkspaceSessionDetails host, null) when string.Equals(headers.WorkspaceId, host.WorkspaceId, StringComparison.InvariantCultureIgnoreCase) && headers.Port == host.Port => headers,
                (WorkspaceSessionDetails headers, null, WorkspaceSessionDetails host) when string.Equals(headers.WorkspaceId, host.WorkspaceId, StringComparison.InvariantCultureIgnoreCase) && headers.Port == host.Port => headers,
                (WorkspaceSessionDetails parameters, null, null) => parameters,
                (null, WorkspaceSessionDetails parameters, null) => parameters,
                (null, null, WorkspaceSessionDetails parameters) => parameters,
                _ => default,
            };

            return sessionDetails != default;
        }
    }
}