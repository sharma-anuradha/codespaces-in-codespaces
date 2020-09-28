// <copyright file="SasController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// Secret stores controller.
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerUtility.UserAuthenticationSchemes)]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("sas_controller")]
    public class SasController : ControllerBase
    {
        private static readonly string GitHubReportingQueueName = "github-reporting-queue";

        /// <summary>
        /// Initializes a new instance of the <see cref="SasController"/> class.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        /// <param name="azureClientFactory">The azure client.</param>
        /// <param name="azureResourceAccessor">The azure resource accessor.</param>
        public SasController(AppSettings appSettings, IAzureClientFactory azureClientFactory, IControlPlaneAzureResourceAccessor azureResourceAccessor)
        {
            AppSettings = appSettings;
            AzureClientFactory = azureClientFactory;
            AzureResourceAccessor = azureResourceAccessor;
        }

        private AppSettings AppSettings { get; }

        private IAzureClientFactory AzureClientFactory { get; }

        private IControlPlaneAzureResourceAccessor AzureResourceAccessor { get; }

        /// <summary>
        /// Fetch all the secrets that the user has access to, for the given plan.
        /// This will include user's personal secrets and the plan level shared secrets if any.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <returns>The IActionResult.</returns>
        [HttpGet]
        [ThrottlePerUserLow(nameof(SasController), nameof(GetDataAsync))]
        [ProducesResponseType(typeof(IEnumerable<ScopedSecretResultBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpOperationalScope("get")]
        public async Task<IActionResult> GetDataAsync([FromServices] IDiagnosticsLogger logger)
        {
            var githubSpId = AppSettings.ControlPlaneSettings.GithubSpId;

            var userid = HttpContext.User.GetUserIdFromClaims();
            if (!userid.Equals(githubSpId))
            {
                return BadRequest();
            }

            var accountToSasDictionary = new Dictionary<string, string>();
            var accounts = await AzureResourceAccessor.GetAllStampStorageAccountForPartner("gh");
            foreach (var (name, key) in accounts)
            {
                // Get queue client for storage account
                var storageCreds = new StorageCredentials(name, key);
                var cloudStorageAccount = new CloudStorageAccount(storageCreds, useHttps: true);

                var queueClient = cloudStorageAccount.CreateCloudQueueClient();

                // Get queue reference
                var queueName = GitHubReportingQueueName;
                var queue = queueClient.GetQueueReference(queueName);

                // Get queue sas token
                var queueSas = queue.GetSharedAccessSignature(new SharedAccessQueuePolicy()
                {
                    Permissions = SharedAccessQueuePermissions.ProcessMessages | SharedAccessQueuePermissions.Read,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(2),
                });
                var token = queueSas.StartsWith("?") ? queueSas.Substring(1) : queueSas;

                accountToSasDictionary.Add(name, token);
            }

            return Ok(accountToSasDictionary);
        }
    }
}