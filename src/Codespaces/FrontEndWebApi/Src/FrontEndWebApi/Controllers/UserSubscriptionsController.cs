// <copyright file="UserSubscriptionsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// Api for adding new users to vsonline.
    /// </summary>
    [ApiController]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("user_subscriptions_controller")]
    public class UserSubscriptionsController : Controller
    {
        /// <summary>
        /// Adds a user.
        /// </summary>
        /// <param name="email">The user email.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The status code.</returns>
        [HttpPost]
        [ThrottlePerUserLow(nameof(UserSubscriptionsController), nameof(CreateAsync))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpOperationalScope("create")]
        public Task<IActionResult> CreateAsync(
            [FromQuery] string email,
            [FromServices] IDiagnosticsLogger logger)
        {
            return Task.FromResult<IActionResult>(Ok());
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
        [HttpOperationalScope("delete")]
        public Task<IActionResult> DeleteAsync(
            [FromQuery] string email,
            [FromServices] IDiagnosticsLogger logger)
        {
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}
