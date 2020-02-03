// <copyright file="DiagnosticsCommandFeedLogger.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using Microsoft.PrivacyServices.CommandFeed.Client;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PcfAgent
{
    /// <summary>
    /// This class allows us to listen to the log events from CommandFeed Framework, and log them using IDiagnosticsLogger.
    /// </summary>
    public class DiagnosticsCommandFeedLogger : CommandFeedLogger
    {
        private readonly IDiagnosticsLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticsCommandFeedLogger"/> class.
        /// </summary>
        /// <param name="diagnosticsLogger">The logger.</param>
        public DiagnosticsCommandFeedLogger(
            IDiagnosticsLogger diagnosticsLogger)
        {
            logger = diagnosticsLogger;
            logger.AddBaseValue(LoggingConstants.Service, "PcfAgent");
        }

        /// <inheritdoc/>
        public override void UnhandledException(Exception ex)
        {
            logger.LogException("pcf_unhandled_exception", ex);
        }

        /// <inheritdoc/>
        public override void SerializationError(object sender, ErrorEventArgs args)
        {
            logger
                .WithValue("sender", $"{sender}")
                .WithValue("object", $"{args}")
                .WithValue("error", $"{args}")
                .LogError("pcf_serialization_error");
        }

        /// <inheritdoc/>
        public override void CommandValidationException(string cv, string commandId, Exception ex)
        {
            logger
                .WithValue("cv", cv)
                .WithValue("command_id", commandId)
                .LogException($"pcf_validation_exception", ex);
        }

        /// <inheritdoc/>
        public override void BatchCompleteError(string commandId, string error)
        {
            logger
                .WithValue("command_id", commandId)
                .WithValue("error", error)
                .LogError("pcf_batch_complete_error");
        }

        /// <inheritdoc/>
        public override void CancellationException(Exception ex)
        {
            logger.LogException("pcf_operation_cancelled", ex);
        }

        /// <inheritdoc/>
        public override void HttpResponseReceived(HttpRequestMessage request, HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return; // Don't log successes because there are many of them and they're not interesting
            }

            logger
                .WithValue("method", $"{request.Method}")
                .WithValue("uri", $"{request.RequestUri}")
                .WithValue("status_code", $"{response.StatusCode}")
                .LogInfo("pcf_http_response_received");
        }

        /// <inheritdoc/>
        public override void BeginServiceToServiceAuthRefresh(string targetSiteName, long siteId)
        {
            logger
                .WithValue("target_site", targetSiteName)
                .WithValue("client_site_id", $"{siteId}")
                .LogInfo("pcf_service_to_service_auth_refresh");
        }

        /// <inheritdoc/>
        public override void UnrecognizedDataType(string cv, string commandId, string dataType)
        {
            logger
                .WithValue("cv", cv)
                .WithValue("command_id", commandId)
                .WithValue("data_type", dataType)
                .LogError("pcf_unrecognized_data_type");
        }
    }
}
