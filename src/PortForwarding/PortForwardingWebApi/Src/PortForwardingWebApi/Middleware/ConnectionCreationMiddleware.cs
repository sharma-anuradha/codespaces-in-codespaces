// <copyright file="ConnectionCreationMiddleware.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using k8s.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.AspNetCore.Http;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Clients;
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
        private const string AuthCookieName = "__Host-vso-pf";

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
        /// <param name="newConnectionsQueueClientProvider">The connections-new queue client provider.</param>
        /// <param name="connectionErrorsSessionClientProvider">The connections-errors session client provider.</param>
        /// <param name="mappingClient">The mappings client.</param>
        /// <param name="hostEnvironment">The host environment.</param>
        /// <param name="hostUtils">The host utils.</param>
        /// <param name="appSettings">The service settings.</param>
        /// <returns>Task.</returns>
        public async Task InvokeAsync(
            HttpContext context,
            INewConnectionsQueueClientProvider newConnectionsQueueClientProvider,
            IConnectionErrorsSessionClientProvider connectionErrorsSessionClientProvider,
            IAgentMappingClient mappingClient,
            IHostEnvironment hostEnvironment,
            PortForwardingHostUtils hostUtils,
            PortForwardingAppSettings appSettings)
        {
            var logger = context.GetLogger();

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

            logger.AddBaseConnectionDetails(connectionInfo);

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
                    var client = await newConnectionsQueueClientProvider.Client.Value;

                    var message = new Message(JsonSerializer.SerializeToUtf8Bytes(connectionInfo, this.serializationOptions))
                    {
                        // Automatic SB deduplication uses message ids to dedupe messages.
                        // We have deduplication set for 30s
                        MessageId = connectionInfo.GetMessagingSessionId(),
                        SessionId = connectionInfo.WorkspaceId,
                        ContentType = MediaTypeNames.Application.Json,
                        CorrelationId = context.GetRequestId(),
                        ReplyTo = QueueNames.ConnectionErrors,
                        ReplyToSessionId = context.GetRequestId(),
                    };

                    await childLogger.OperationScopeAsync("connection_creation_middleware_connections_new_send_async", (_) => client.SendAsync(message));
                });

            // 3. Wait for the connection kubernetes service is available
            // Note:
            // The service is created by EstablishedConnectionsWorker based on "connection-established" message from PFA.
            // Having service creation and responding to requests separate allows us to respond to multiple requests, not just the first one.
            try
            {
                var errorMessageTask = logger.OperationScopeAsync("connection_creation_middleware_subscribe_to_errors", async (childLogger) =>
                {
                    var errorsSessionsClient = await connectionErrorsSessionClientProvider.Client.Value;

                    try
                    {
                        var session = await errorsSessionsClient.AcceptMessageSessionAsync(context.GetCorrelationId(), TimeSpan.FromSeconds(60));
                        var message = await session.ReceiveAsync();
                        if (message != default)
                        {
                            await session.CompleteAsync(GetLockToken(message));

                            return JsonSerializer.Deserialize<ErrorMessage>(message.Body, this.serializationOptions);
                        }

                        return null;
                    }
                    catch (Exception ex)
                    {
                        logger.AddExceptionInfo(ex);
                        logger.LogWarning("connection_creation_middleware_subscribe_to_errors_failed");

                        return null;
                    }
                });

                var waitForEndpointTask = mappingClient.WaitForEndpointReadyAsync(connectionInfo.GetKubernetesServiceName(), logger);
                var endpointOrErrorTask = await Task.WhenAny(
                     Wrap(waitForEndpointTask),
                     Wrap(errorMessageTask));

                // When for some reason we fail to acquire the error message and it completes
                // before fetching endpoint, we still want to wait for the endpoint or time out.
                var result = await endpointOrErrorTask;
                if (result == (null, null) && !waitForEndpointTask.IsCompletedSuccessfully)
                {
                    result = await Wrap(waitForEndpointTask);
                }

                switch (result)
                {
                    case (V1Endpoints endpoint, null):
                        var ip = endpoint.Subsets.First().Addresses.First().Ip;
                        var port = endpoint.Subsets.First().Ports.First().Port;

                        var uriBuilder = new UriBuilder(context.Request.Scheme, ip, port)
                        {
                            Path = context.Request.Path,
                            Query = context.Request.QueryString.ToString(),
                        };

                        logger.AddValue("TargetUrl", uriBuilder.Uri.ToString());
                        logger.LogInfo("connection_creation_middleware_redirect");

                        context.Response.Redirect(uriBuilder.Uri.ToString());
                        await context.Response.CompleteAsync();

                        break;
                    case (null, ErrorMessage message):
                        logger.LogErrorWithDetail("connection_creation_middleware_agent_error", message.Detail);
                        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

                        // Invalidate PF auth cookie so next time user goes through auth flow.
                        context.Response.Cookies.Append(AuthCookieName, "expired", new CookieOptions { Expires = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(2)) });

                        if (hostEnvironment.IsDevelopment())
                        {
                            context.Response.ContentType = MediaTypeNames.Application.Json;
                            await context.Response.WriteAsync(JsonSerializer.Serialize(message));
                        }

                        await context.Response.CompleteAsync();

                        break;
                    default:
                        throw new OperationCanceledException();
                }
            }
            catch (Exception ex) when (ex is TaskCanceledException || ex is OperationCanceledException)
            {
                context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                logger.LogWarning("connection_creation_middleware_service_creation_timeout");
                await context.Response.CompleteAsync();
            }
        }

        private async Task<(V1Endpoints? Endpoint, ErrorMessage? Message)> Wrap(Task<V1Endpoints> endpointTask)
        {
            var res = await endpointTask;

            return (res, default);
        }

        private async Task<(V1Endpoints? Endpoint, ErrorMessage? Message)> Wrap(Task<ErrorMessage?> endpointTask)
        {
            var res = await endpointTask;

            return (default, res);
        }

        private string? GetLockToken(Message message)
        {
            return message.SystemProperties.IsLockTokenSet ? message.SystemProperties.LockToken : null;
        }
    }
}
