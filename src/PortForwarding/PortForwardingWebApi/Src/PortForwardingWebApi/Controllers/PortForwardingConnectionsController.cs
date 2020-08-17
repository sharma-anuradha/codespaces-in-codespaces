// <copyright file="PortForwardingConnectionsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net.Mime;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using k8s;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VsSaaS.AspNetCore.Http;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.CodespacesApiClient;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.Common.Clients;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Controllers
{
    /// <summary>
    /// Exposes API for port forwarding service connection management.
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerUtility.UserAuthenticationSchemes)]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("port_forwarding_connections")]
    public class PortForwardingConnectionsController : Controller
    {
        private readonly JsonSerializerOptions serializationOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        /// <summary>
        /// Initializes a new instance of the <see cref="PortForwardingConnectionsController"/> class.
        /// </summary>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="newConnectionsQueueClientProvider">The connections-new queue client provider.</param>
        /// <param name="kubernetesClient">The kubernetes client.</param>
        /// <param name="appSettings">The settings.</param>
        /// <param name="frontEndClient">The front end service client.</param>
        public PortForwardingConnectionsController(
            ICurrentUserProvider currentUserProvider,
            INewConnectionsQueueClientProvider newConnectionsQueueClientProvider,
            IKubernetes kubernetesClient,
            PortForwardingAppSettings appSettings,
            ICodespacesApiClient frontEndClient)
        {
            CurrentUserProvider = currentUserProvider;
            NewConnectionsQueueClientProvider = newConnectionsQueueClientProvider;
            KubernetesClient = kubernetesClient;
            AppSettings = appSettings;
            FrontEndClient = frontEndClient;
        }

        private ICurrentUserProvider CurrentUserProvider { get; }

        private INewConnectionsQueueClientProvider NewConnectionsQueueClientProvider { get; }

        private IKubernetes KubernetesClient { get; }

        private PortForwardingAppSettings AppSettings { get; }

        private ICodespacesApiClient FrontEndClient { get; }

        /// <summary>
        /// Get connection status.
        /// </summary>
        /// <param name="codespaceId">The codespaceId.</param>
        /// <param name="port">The port.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>Status code.</returns>
        [HttpGet("{codespaceId:guid}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpOperationalScope("get_codespace_connection_status")]
        public async Task<IActionResult> GetCodespaceConnectionStatusAsync(
            string codespaceId,
            [FromQuery(Name = "port")] int port,
            [FromServices] IDiagnosticsLogger logger)
        {
            var codespace = await FrontEndClient.GetCodespaceAsync(codespaceId, logger);
            if (codespace == default)
            {
                return NotFound();
            }

            var kubernetesObjectName = $"pf-{codespace.Connection.ConnectionSessionId.ToLower()}-{port}";

            var ingressRule = await logger.OperationScopeAsync(
                "check_ingress",
                (childLogger) => KubernetesClient.ReadNamespacedIngressAsync(kubernetesObjectName, "default"),
                swallowException: true);

            if (ingressRule == default)
            {
                return NotFound();
            }

            return Ok();
        }

        /// <summary>
        /// Get connection status.
        /// </summary>
        /// <param name="connectionId">The codespaceId.</param>
        /// <param name="port">The port.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>Status code.</returns>
        [HttpGet("{connectionId:regex([[0-9A-Fa-f]]{{36}})}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpOperationalScope("get_codespace_connection_status")]
        public async Task<IActionResult> GetLiveShareWorkspaceConnectionStatusAsync(
            string connectionId,
            [FromQuery(Name = "port")] int port,
            [FromServices] IDiagnosticsLogger logger)
        {
            var kubernetesObjectName = $"pf-{connectionId.ToLower()}-{port}";

            var ingressRule = await logger.OperationScopeAsync(
                "check_ingress",
                (childLogger) => KubernetesClient.ReadNamespacedIngressAsync(kubernetesObjectName, "default"),
                swallowException: true);

            if (ingressRule == default)
            {
                return NotFound();
            }

            return Ok();
        }

        /// <summary>
        /// Creates new port forwarding service connection.
        /// </summary>
        /// <param name="requestBody">The connected request body.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The url for status polling.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpOperationalScope("create_codespace_connection")]
        public async Task<IActionResult> PostConnectionAsync(ConnectionCreationRequest requestBody, [FromServices] IDiagnosticsLogger logger)
        {
            string workspaceId;
            string? environmentId = null;
            string liveshareEndpoint = AppSettings.VSLiveShareApiEndpoint;
            if (Regex.IsMatch(requestBody.Id, PortForwardingHostUtils.EnvironmentIdRegex))
            {
                environmentId = requestBody.Id;

                var codespace = await FrontEndClient.GetCodespaceAsync(requestBody.Id, logger);
                if (codespace == default)
                {
                    return BadRequest();
                }

                liveshareEndpoint = codespace.Connection.ConnectionServiceUri;
                workspaceId = codespace.Connection.ConnectionSessionId;
            }
            else if (Regex.IsMatch(requestBody.Id, PortForwardingHostUtils.WorkspaceIdRegex))
            {
                workspaceId = requestBody.Id;
            }
            else
            {
                return BadRequest();
            }

            var connectionInfo = new ConnectionRequest
            {
                EnvironmentId = environmentId,
                WorkspaceId = workspaceId,
                VSLiveShareApiEndpoint = liveshareEndpoint,
                Port = requestBody.Port,
                Token = CurrentUserProvider.BearerToken,
            };

            await logger.OperationScopeAsync(
                "send_create_new_connection_message",
                async (childLogger) =>
                {
                    childLogger.AddConnectionDetails(connectionInfo);

                    var client = await NewConnectionsQueueClientProvider.Client.Value;

                    var message = new Message(JsonSerializer.SerializeToUtf8Bytes(connectionInfo, this.serializationOptions))
                    {
                        SessionId = connectionInfo.WorkspaceId,
                        ContentType = MediaTypeNames.Application.Json,
                        CorrelationId = HttpContext.GetCorrelationId(),
                        ReplyTo = QueueNames.ConnectionErrors,
                        ReplyToSessionId = HttpContext.GetCorrelationId(),
                    };

                    await childLogger.OperationScopeAsync("send_create_new_connection_message_send_async", (_) => client.SendAsync(message));
                });

            return Ok();
        }
    }
}
