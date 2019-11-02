// <copyright file="UserSubscriptionsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
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
    [Route("usersubscriptions")]
    [Authorize(AuthenticationSchemes = AuthenticationBuilderJwtExtensions.AuthenticationScheme)]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("userSubscriptions_controller")]
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
        /// <returns>The status code.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> AddSubscription([FromQuery] string email)
        {
            var logger = HttpContext.GetLogger();

            await logger.OperationScopeAsync(
                $"{this.GetType().GetLogMessageBaseName()}_add_user_subscription",
                async (childLogger) =>
                {
                    ValidationUtil.IsRequired(email, nameof(email));
                    var currentUserId = CurrentUserProvider.GetProfileId();
                    var us = new UserSubscription()
                    {
                        Id = email,
                        UserId = currentUserId,
                        Timestamp = DateTime.UtcNow,
                    };

                    await this.UserSubscriptionRepository.CreateOrUpdateAsync(us, logger);
                });

            return Ok();
        }

        /// <summary>
        /// Deletes a user.
        /// </summary>
        /// <param name="email">The email address.</param>
        /// <returns>The status code.</returns>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DeleteSubscription([FromQuery] string email)
        {
            var logger = HttpContext.GetLogger();

            await logger.OperationScopeAsync(
                $"{this.GetType().GetLogMessageBaseName()}_delete_user_subscription",
                async (childLogger) =>
                {
                    ValidationUtil.IsRequired(email, nameof(email));

                    await this.UserSubscriptionRepository.DeleteAsync(email, logger);
                });

            return Ok();
        }
    }
}
