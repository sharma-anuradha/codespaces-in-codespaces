// <copyright file="DefaultController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.VsCloudKernel.SignalService.Controllers
{
    /// <summary>
    /// This controller will be hit on the root Uri and typically will be coming from the nginx controller when
    /// hosted in AKS.
    /// </summary>
    public class DefaultController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}
