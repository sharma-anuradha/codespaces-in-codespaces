// <copyright file="MeController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.AspNetCore.Http;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// The cloud environment API controller.
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerUtility.AuthenticationScheme)]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("me_controller")]
    public class MeController : ControllerBase /* TODO add this later IEnvironmentsHttpContract */
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MeController"/> class.
        /// </summary>
        /// <param name="currentUserProvider">The current user provider.</param>
        public MeController(
            ICurrentUserProvider currentUserProvider)
        {
            CurrentUserProvider = Requires.NotNull(currentUserProvider, nameof(currentUserProvider));
        }

        private ICurrentUserProvider CurrentUserProvider { get; }

        /// <summary>
        /// Get an environment by id.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="hostingEnvironment">The aspnetcore hosting environment.</param>
        /// <returns>An object result containing the <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpGet]
        [ThrottlePerUserHigh(nameof(MeController), nameof(GetAsync))]
        [ProducesResponseType(typeof(MeResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpOperationalScope("get")]
        public async Task<IActionResult> GetAsync(
            [FromRoute] string environmentId,
            [FromServices] IWebHostEnvironment hostingEnvironment)
        {
            await Task.CompletedTask;

            var profile = CurrentUserProvider.GetProfile();

            // Only reveal the idMapKey for diagnostics purposes
            var idMapKey = default(string);
            if (hostingEnvironment.IsDevelopment())
            {
                idMapKey = CurrentUserProvider.GetIdMapKey();
            }

            var avatarUri = profile?.AvatarUri;
            var canonicalUserId = HttpContext.GetCurrentUserCanonicalUserId();
            var displayName = HttpContext.GetCurrentUserDisplayName();
            var email = HttpContext.GetCurrentUserEmail();
            var userId = HttpContext.GetCurrentUserId();
            var profileId = HttpContext.GetCurrentUserProfileId();
            var providerId = HttpContext.GetCurrentUserProfileProviderId();

            var result = new MeResult
            {
                DisplayName = displayName,
                AvatarUri = avatarUri,
                Email = email,
                PreferredUserId = userId,
                CanonicalUserId = canonicalUserId,
                ProfileId = profileId,
                ProfileProviderId = providerId,
                IdMapKey = idMapKey,
            };

            return Ok(result);
        }
    }
}
