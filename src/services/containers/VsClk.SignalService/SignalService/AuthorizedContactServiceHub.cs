// <copyright file="AuthorizedContactServiceHub.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// A presence service hub class that use the Jwt bear authentication scheme
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AuthorizedContactServiceHub : ContactServiceHub
    {
        public AuthorizedContactServiceHub(ContactService presenceService, ILogger<ContactServiceHub> logger, IDataFormatProvider formatProvider = null)
            : base(presenceService, logger, formatProvider)
        {
        }

        protected override string GetContactIdentity(string contactId)
        {
            // enforce always to use the Claims parameter from the context call
            return Context?.User?.FindFirst("userId")?.Value ?? contactId;
        }
    }
}
