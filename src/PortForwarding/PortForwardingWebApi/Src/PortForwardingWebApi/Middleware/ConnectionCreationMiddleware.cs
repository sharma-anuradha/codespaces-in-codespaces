// <copyright file="ConnectionCreationMiddleware.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.ServiceBus;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Middleware
{
    /// <summary>
    /// Middleware to handle creating new LS connections and routing traffic.
    /// </summary>
    public class ConnectionCreationMiddleware
    {
        private readonly JsonSerializerOptions serializationOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionCreationMiddleware"/> class.
        /// </summary>
        /// <param name="next">Next middleware delegate.</param>
        public ConnectionCreationMiddleware(RequestDelegate next)
        {
            Next = next;
        }

        private RequestDelegate Next { get; }

        /// <summary>
        /// Handles the first PFS client request.
        /// </summary>
        /// <param name="context">Request context.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="queueClientProvider">Queue client provider.</param>
        /// <param name="mappingClient">The mappings client.</param>
        /// <param name="appSettings">The service settings.</param>
        /// <returns>Task.</returns>
        public async Task InvokeAsync(
            HttpContext context,
            IDiagnosticsLogger logger,
            IServiceBusQueueClientProvider queueClientProvider,
            IAgentMappingClient mappingClient,
            PortForwardingAppSettings appSettings)
        {
            // 1. Extract connection information from the request context.
            string workspaceId = string.Empty;
            string portString = string.Empty;
            string token = string.Empty;
            int port;

            if (!context.Request.Headers.TryGetValue("X-VSOnline-Forwarding-WorkspaceId", out var workspaceIdValues) ||
                !context.Request.Headers.TryGetValue("X-VSOnline-Forwarding-Port", out var portStringValues))
            {
                var hostString = context.Request.Host.ToString();

                var routingHostPartRegex = "(?<workspaceId>[0-9A-Fa-f]{36})-(?<port>\\d{2,5})";
                var hostRegexes = appSettings.HostsConfigs.SelectMany(hostConf => hostConf.Hosts.Select(host => string.Format(host, routingHostPartRegex)));
                var currentHostRegex = hostRegexes.SingleOrDefault(reg => Regex.IsMatch(hostString, reg));
                if (currentHostRegex != default)
                {
                    var match = Regex.Match(hostString, currentHostRegex);
                    workspaceId = match.Groups["workspaceId"].Value;
                    portString = match.Groups["port"].Value;
                }
            }
            else
            {
                workspaceId = workspaceIdValues.FirstOrDefault();
                portString = portStringValues.FirstOrDefault();
            }

            if (context.Request.Headers.TryGetValue("X-VSOnline-Forwarding-Token", out var tokenValues))
            {
                token = tokenValues.FirstOrDefault();
            }

            if (!Regex.IsMatch(workspaceId, "^[0-9A-Fa-f]{36}$") || !int.TryParse(portString, out port) || string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.CompleteAsync();

                return;
            }

            // 2. Send the ConnectionRequest message to the queue
            // Note:
            // This message will be picked up by a Port Forwarding Agent and new LiveShare port forwarding session will be established.
            var connectionInfo = new ConnectionRequest
            {
                WorkspaceId = workspaceId,
                Port = port,
                Token = token,
            };

            await logger.OperationScopeAsync(
                "connection_creation_middleware_send_create_new_message",
                async (childLogger) =>
                {
                    var client = await queueClientProvider.GetQueueClientAsync("connections-new", childLogger);

                    var message = new Message(JsonSerializer.SerializeToUtf8Bytes(connectionInfo, serializationOptions))
                    {
                        SessionId = workspaceId,
                    };

                    await client.SendAsync(message);
                });

            // 3. Wait for the connection kubernetes service is available
            // Note:
            // The service is created by EstablishedConnectionsWorker based on "connection-established" message from PFA.
            // Having service creation and responding to requests separate allows us to respond to multiple requests, not just the first one.
            try
            {
                await mappingClient.WaitForServiceAvailableAsync(connectionInfo.GetKubernetesServiceName(), logger);
            }
            catch (TaskCanceledException)
            {
                context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                context.Response.Headers.Add("X-Powered-By", "Visual Studio Online Portal");
                await context.Response.CompleteAsync();

                return;
            }

            var uriBuilder = new UriBuilder(
                context.Request.Scheme,
                $"{connectionInfo.GetKubernetesServiceName()}.svc.cluster.local");

            uriBuilder.Path = context.Request.Path;
            uriBuilder.Query = context.Request.QueryString.ToString();

            context.Response.Redirect(uriBuilder.Uri.ToString());

            await Next(context);
        }
    }
}
