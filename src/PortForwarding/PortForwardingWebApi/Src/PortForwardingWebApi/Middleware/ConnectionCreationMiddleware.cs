// <copyright file="ConnectionCreationMiddleware.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.ServiceBus;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.ServiceBus;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Models;
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
        /// <param name="hostUtils">The host utils.</param>
        /// <param name="appSettings">The service settings.</param>
        /// <returns>Task.</returns>
        public async Task InvokeAsync(
            HttpContext context,
            IDiagnosticsLogger logger,
            IServiceBusQueueClientProvider queueClientProvider,
            IAgentMappingClient mappingClient,
            PortForwardingHostUtils hostUtils,
            PortForwardingAppSettings appSettings)
        {
            // 1. Extract connection information from the request context.
            var token = string.Empty;
            if (context.Request.Headers.TryGetValue(PortForwardingHeaders.Token, out var tokenValues))
            {
                token = tokenValues.SingleOrDefault();
            }

            if (string.IsNullOrEmpty(token))
            {
                logger.LogInfo("connection_creation_middleware_missing_token");

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.CompleteAsync();

                return;
            }

            // 2. Send the ConnectionRequest message to the queue
            // Note:
            // This message will be picked up by a Port Forwarding Agent and new LiveShare port forwarding session will be established.
            var connectionInfo = new ConnectionRequest
            {
                Token = token,
                VSLiveShareApiEndpoint = appSettings.VSLiveShareApiEndpoint,
            };

            // At this stage we only care about headers setting the PF context.
            // TODO: Can we structure the helpers and services in a way that PFS would explicitly work only on top of headers?
            hostUtils.TryGetPortForwardingSessionDetails(context.Request, out var sessionDetails);
            switch (sessionDetails)
            {
                case EnvironmentSessionDetails details:
                    connectionInfo.WorkspaceId = details.WorkspaceId;
                    connectionInfo.EnvironmentId = details.EnvironmentId;
                    connectionInfo.Port = details.Port;
                    break;
                case WorkspaceSessionDetails details:
                    connectionInfo.WorkspaceId = details.WorkspaceId;
                    connectionInfo.Port = details.Port;
                    break;
                default:
                    logger.LogInfo("connection_creation_middleware_missing_or_invalid_session_details");

                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.CompleteAsync();
                    return;
            }

            await logger.OperationScopeAsync(
                "connection_creation_middleware_send_create_new_message",
                async (childLogger) =>
                {
                    var client = await queueClientProvider.GetQueueClientAsync(QueueNames.NewConnections, childLogger);

                    var message = new Message(JsonSerializer.SerializeToUtf8Bytes(connectionInfo, serializationOptions))
                    {
                        SessionId = connectionInfo.WorkspaceId,
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
            catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
            {
                context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                context.Response.Headers.Add("X-Powered-By", "Visual Studio Online");
                await context.Response.CompleteAsync();

                return;
            }

            var uriBuilder = new UriBuilder(
                context.Request.Scheme,
                $"{connectionInfo.GetKubernetesServiceName()}.default.svc.cluster.local")
            {
                Path = context.Request.Path,
                Query = context.Request.QueryString.ToString(),
            };

            logger.AddValue("target_url", uriBuilder.Uri.ToString());
            logger.LogInfo("connection_creation_middleware_redirect");
            context.Response.Redirect(uriBuilder.Uri.ToString());
            await context.Response.CompleteAsync();
        }
    }
}