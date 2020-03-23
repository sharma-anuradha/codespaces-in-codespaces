﻿// <copyright file="ConnectionCreationMiddleware.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings;

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
        /// <param name="hostUtils">the host utils.</param>
        /// <returns>Task.</returns>
        public async Task InvokeAsync(
            HttpContext context,
            IDiagnosticsLogger logger,
            IServiceBusQueueClientProvider queueClientProvider,
            IAgentMappingClient mappingClient,
            PortForwardingHostUtils hostUtils)
        {
            // 1. Extract connection information from the request context.
            var token = string.Empty;
            if (context.Request.Headers.TryGetValue(PortForwardingHeaders.Token, out var tokenValues))
            {
                token = tokenValues.FirstOrDefault();
            }

            if (string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.CompleteAsync();

                return;
            }

            (string WorkspaceId, int Port) sessionDetails = default;

            if (context.Request.Headers.TryGetValue(PortForwardingHeaders.WorkspaceId, out var workspaceIdValues) &&
                context.Request.Headers.TryGetValue(PortForwardingHeaders.Port, out var portStringValues) &&
                !hostUtils.TryGetPortForwardingSessionDetails(
                    workspaceIdValues.FirstOrDefault(),
                    portStringValues.FirstOrDefault(),
                    out sessionDetails))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.CompleteAsync();

                return;
            }

            if (sessionDetails == default && !hostUtils.TryGetPortForwardingSessionDetails(context.Request.Host.ToString(), out sessionDetails))
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
                WorkspaceId = sessionDetails.WorkspaceId,
                Port = sessionDetails.Port,
                Token = token,
            };

            await logger.OperationScopeAsync(
                "connection_creation_middleware_send_create_new_message",
                async (childLogger) =>
                {
                    var client = await queueClientProvider.GetQueueClientAsync(QueueNames.NewConnections, childLogger);

                    var message = new Message(JsonSerializer.SerializeToUtf8Bytes(connectionInfo, serializationOptions))
                    {
                        SessionId = sessionDetails.WorkspaceId,
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
                context.Response.Headers.Add("X-Powered-By", "Visual Studio Online");
                await context.Response.CompleteAsync();

                return;
            }

            var uriBuilder = new UriBuilder(
                context.Request.Scheme,
                $"{connectionInfo.GetKubernetesServiceName()}.svc.cluster.local")
            {
                Path = context.Request.Path,
                Query = context.Request.QueryString.ToString(),
            };

            context.Response.Redirect(uriBuilder.Uri.ToString());

            await Next(context);
        }
    }
}