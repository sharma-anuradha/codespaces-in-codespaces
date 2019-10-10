// <copyright file="AccountsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// The cloud environment API controller.
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthenticationBuilderJwtExtensions.AuthenticationScheme)]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("accounts_controller")]
    public class AccountsController : ControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountsController"/> class.
        /// </summary>
        /// <param name="accountManager">The front-end account manager.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="mapper">The configured auto-mapper.</param>
        public AccountsController(
            IAccountManager accountManager,
            ICurrentUserProvider currentUserProvider)
        {
            AccountManager = Requires.NotNull(accountManager, nameof(accountManager));
            CurrentUserProvider = Requires.NotNull(currentUserProvider, nameof(currentUserProvider));
        }

        private IAccountManager AccountManager { get; }

        private ICurrentUserProvider CurrentUserProvider { get; }

        /// <summary>
        /// Get an account by id.
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription containing the account.</param>
        /// <param name="resourceGroupName">The name of the resource group containing the account.</param>
        /// <param name="resourceName">The name of the account resource.</param>
        /// <returns>An object result containing the <see cref="AccountResult"/>.</returns>
        [HttpGet("{subscriptionId}/{resourceGroupName}/{resourceName}")]
        [ProducesResponseType(typeof(AccountResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAccountAsync(
            [FromRoute]string subscriptionId,
            [FromRoute]string resourceGroupName,
            [FromRoute]string resourceName)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
            {
                var currentUserId = CurrentUserProvider.GetProfileId();
                var accountId = new VsoAccountInfo
                {
                    Subscription = subscriptionId,
                    ResourceGroup = resourceGroupName,
                    Name = resourceName,
                };

                var account = await AccountManager.GetAsync(accountId, logger);

                if (account == null || account.UserId != currentUserId)
                {
                    logger.AddDuration(duration)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogError(GetType().FormatLogErrorMessage(nameof(GetAccountAsync)));
                    return NotFound();
                }

                var result = MapAccountToResult(account, logger);
                if (result == null)
                {
                    logger.AddDuration(duration)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogError(GetType().FormatLogErrorMessage(nameof(GetAccountAsync)));
                    return NotFound();
                }

                logger.AddDuration(duration)
                    .LogInfo(GetType().FormatLogMessage(nameof(GetAccountAsync)));
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetAccountAsync)), ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Lists all accounts belonging to the current user.
        /// </summary>
        /// <returns>An object result containing the list of <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(AccountResult[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ListAccountsByOwnerAsync()
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
            {
                // Match on provider ID instead of profile ID because clients dont have
                // the profile ID when the create the account resource via ARM.
                // (The provider ID is a combination of "tid" and "oid" claims from the token.)
                var currentUserProviderId = CurrentUserProvider.GetProfile().ProviderId;
                var accounts = await AccountManager.ListAsync(
                    currentUserProviderId, subscriptionId: null, resourceGroup: null, logger);

                logger.AddDuration(duration)
                    .LogInfo(GetType().FormatLogMessage(nameof(ListAccountsByOwnerAsync)));
                return Ok(accounts.Select((a) => MapAccountToResult(a, logger))
                    .Where((a) => a != null).ToArray());
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(ListAccountsByOwnerAsync)), ex.Message);
                throw;
            }
        }

        private AccountResult MapAccountToResult(VsoAccount account, IDiagnosticsLogger logger)
        {
            try
            {
                // Can't use AutoMapper because ResourceId is a computed property.
                var a = account.Account;
                return new AccountResult
                {
                    Id = a.ResourceId,
                    Subscription = a.Subscription,
                    ResourceGroup = a.ResourceGroup,
                    Name = a.Name,
                    Location = a.Location,
                };
            }
            catch (Exception ex)
            {
                logger.LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(MapAccountToResult)), ex.Message);

                // ResourceId is a computed property and may throw.
                // In that case, skip this item and still return the others.
                return null;
            }
        }
    }
}
