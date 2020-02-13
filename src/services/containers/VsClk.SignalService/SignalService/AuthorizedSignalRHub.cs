// <copyright file="AuthorizedSignalRHub.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// A presence service hub class that use the Jwt bear authentication scheme
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AuthorizedSignalRHub : SignalRHub
    {
        public AuthorizedSignalRHub(
            IServiceScopeFactory serviceScopeFactory,
            IEnumerable<HubDispatcher> hubDispatchers)
            : base(serviceScopeFactory, hubDispatchers)
        {
        }
    }
}
