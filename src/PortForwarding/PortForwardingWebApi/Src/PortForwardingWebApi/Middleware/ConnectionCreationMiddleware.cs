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
using Microsoft.Rest;
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

            var connectionInfo = new ConnectionRequest
            {
                WorkspaceId = workspaceId,
                Port = port,
                Token = token,
            };

            try
            {
                var session = await logger.OperationScopeAsync<IMessageSession>(
                    "connection_creation_middleware_lock_connection_established_session",
                    async (childLogger) =>
                    {
                        var client = await queueClientProvider.GetSessionClientAsync("connections-established", childLogger);
                        return await client.AcceptMessageSessionAsync(connectionInfo.GetMessagingSessionId(), TimeSpan.FromSeconds(60));
                    });

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

                await logger.OperationScopeAsync(
                    "connection_creation_middleware_subscribe_connection_establishing",
                    async (childLogger) =>
                    {
                        var message = await session.ReceiveAsync(TimeSpan.FromSeconds(60));
                        if (message != default && message.Label == MessageLabels.ConnectionEstablishing)
                        {
                            var mapping = JsonSerializer.Deserialize<ConnectionDetails>(message.Body, serializationOptions);

                            try
                            {
                                await mappingClient.CreateAgentConnectionMappingAsync(mapping, childLogger.NewChildLogger());
                            }
                            catch (HttpOperationException ex)
                            {
                                // Handle only conflict exceptions from Kubernetes (the HttpOperationException).
                                childLogger.LogException("connection_creation_middleware_create_kubernetes_objects_failed", ex);
                            }
                        }
                    });

                await logger.OperationScopeAsync(
                    "connection_creation_middleware_subscribe_connection_established",
                    async (childLogger) =>
                    {
                        var message = await session.ReceiveAsync(TimeSpan.FromSeconds(60));
                        if (message != default && message.Label == MessageLabels.ConnectionEstablished)
                        {
                            var mapping = JsonSerializer.Deserialize<ConnectionDetails>(message.Body, serializationOptions);

                            try
                            {
                                var uriBuilder = new UriBuilder(
                                    context.Request.Scheme,
                                    $"{mapping.GetKubernetesServiceName()}.svc.cluster.local");

                                uriBuilder.Path = context.Request.Path;
                                uriBuilder.Query = context.Request.QueryString.ToString();

                                context.Response.Redirect(uriBuilder.Uri.ToString());
                                return;
                            }
                            catch (HttpOperationException ex)
                            {
                                childLogger.LogException("connection_creation_middleware_create_kubernetes_objects_failed", ex);
                            }
                        }
                    });
            }
            catch (SessionCannotBeLockedException)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.CompleteAsync();

                return;
            }

            // Call the next delegate/middleware in the pipeline
            await Next(context);
        }
    }
}
