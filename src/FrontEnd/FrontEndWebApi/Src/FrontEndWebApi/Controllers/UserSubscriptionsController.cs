// <copyright file="UserSubscriptionsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserSubscriptions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// Api for adding new users to vsonline.
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerUtility.AuthenticationScheme)]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("user_subscriptions_controller")]
    public class UserSubscriptionsController : Controller
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserSubscriptionsController"/> class.
        /// </summary>
        /// <param name="userSubscriptionRepository">The repository with user subscriptions.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        public UserSubscriptionsController(
            IUserSubscriptionRepository userSubscriptionRepository,
            ICurrentUserProvider currentUserProvider)
        {
            UserSubscriptionRepository = Requires.NotNull(userSubscriptionRepository, nameof(userSubscriptionRepository));
            CurrentUserProvider = Requires.NotNull(currentUserProvider, nameof(currentUserProvider));
        }

        private ICurrentUserProvider CurrentUserProvider { get; }

        private IUserSubscriptionRepository UserSubscriptionRepository { get; }

        /// <summary>
        /// Adds a user.
        /// </summary>
        /// <param name="email">The user email.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The status code.</returns>
        [HttpPost]
        [ThrottlePerUserLow(nameof(UserSubscriptionsController), nameof(CreateAsync))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpOperationalScope("create")]
        public async Task<IActionResult> CreateAsync(
            [FromQuery]string email,
            [FromServices]IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(email, nameof(email));
            var currentUserId = CurrentUserProvider.GetCurrentUserIdSet().PreferredUserId;
            var us = new UserSubscription()
            {
                Id = email,
                UserId = currentUserId,
                Timestamp = DateTime.UtcNow,
            };

            await UserSubscriptionRepository.CreateOrUpdateAsync(us, logger);

            return Ok();
        }

        /// <summary>
        /// Deletes a user.
        /// </summary>
        /// <remarks>
        /// Note: This endpoint allows the deletion of the email address someone else added in the database.
        /// </remarks>
        /// <param name="email">The email address.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The status code.</returns>
        [HttpDelete]
        [ThrottlePerUserLow(nameof(UserSubscriptionsController), nameof(DeleteAsync))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpOperationalScope("delete")]
        public async Task<IActionResult> DeleteAsync(
            [FromQuery]string email,
            [FromServices]IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(email, nameof(email));

            await UserSubscriptionRepository.DeleteAsync(email, logger);

            return Ok();
        }
    }
}
