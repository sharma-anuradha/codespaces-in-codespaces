// <copyright file="InfoController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Warmup;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Controllers
{
    /// <summary>
    /// This controller is used to warmup dependent service connections.
    /// We use this to prevent traffic from hitting the service until everything is ready.
    /// </summary>
    [ApiController]
    [Route(ServiceConstants.ApiV1Route)]
    public class InfoController : ControllerBase
    {
        /// <summary>
        /// Status of the warm up.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Info Get()
        {
            return new Info
            {
                Message = "Hello, World!",
            };
        }
    }
}
