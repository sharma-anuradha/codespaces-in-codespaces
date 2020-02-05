// <copyright file="ImagesController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Images;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackendWebApi.Controllers
{
    /// <summary>
    /// This controller is used to provide information about the images that are current in use in this control plane location.
    /// </summary>
    [ApiController]
    [Route(ImagesHttpContract.ImagesControllerRoute)]
    [LoggingBaseName("backend_images_controller")]
    public class ImagesController : Controller
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImagesController"/> class.
        /// </summary>
        /// <param name="imageProvider">The image provider with current image information.</param>
        public ImagesController(
             ICurrentImageInfoProvider imageProvider)
        {
            ImageProvider = imageProvider;
        }

        private ICurrentImageInfoProvider ImageProvider { get; }

        /// <summary>
        /// Gets the value of a property related to a specific image family.
        /// </summary>
        /// <param name="imageType">The image family type.</param>
        /// <param name="family">The image family.</param>
        /// <param name="property">The property to query. Should be either "name" or "version".</param>
        /// <param name="defaultValue">The default value to use if no override is found.</param>
        /// <param name="logger">The logger to use for this operation.</param>
        /// <returns>The value that should be used.</returns>
        [HttpGet(ImagesHttpContract.GetImageRoute)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpOperationalScope("get")]
        public async Task<IActionResult> GetAsync(
            ImageFamilyType imageType,
            string family,
            string property,
            string defaultValue,
            [FromServices]IDiagnosticsLogger logger)
        {
            if (imageType != ImageFamilyType.Compute &&
                imageType != ImageFamilyType.Storage &&
                imageType != ImageFamilyType.VmAgent)
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: {nameof(imageType)} is invalid ('{imageType}')");
                return BadRequest();
            }

            if (string.IsNullOrEmpty(family))
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: {nameof(family)} is null or empty");
                return BadRequest();
            }

            if (string.IsNullOrEmpty(property))
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: {nameof(property)} is null or empty");
                return BadRequest();
            }

            if (property != "name" && property != "version")
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: {nameof(property)} is invalid ('{property}'). The only allowed properties are 'name' or 'version'");
                return BadRequest();
            }

            if (string.IsNullOrEmpty(defaultValue))
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: {nameof(defaultValue)} is null or empty");
                return BadRequest();
            }

            var result = defaultValue;
            switch (property)
            {
                case "name":
                    result = await ImageProvider.GetImageNameAsync(imageType, family, defaultValue, logger);
                    break;
                case "version":
                    result = await ImageProvider.GetImageVersionAsync(imageType, family, defaultValue, logger);
                    break;
            }

            return Ok(result);
        }
    }
}
