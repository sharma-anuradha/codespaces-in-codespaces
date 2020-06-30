// <copyright file="PlanResourceHeaders.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models
{
    /// <summary>
    /// Custom headers passed by RPSaaS.
    /// </summary>
    public class PlanResourceHeaders
    {
        private const string HomeTenantIdHeaderName = "x-ms-home-tenant-id";
        private const string ClientTenantIdHeaderName = "x-ms-client-tenant-id";
        private const string IdentityUrlHeaderName = "x-ms-identity-url";
        private const string IdentityPrincipalIdHeaderName = "x-ms-identity-principal-id";

        /// <summary>
        /// Gets or sets the home tenant ID.
        /// </summary>
        [FromHeader(Name = HomeTenantIdHeaderName)]
        public Guid? HomeTenantId { get; set; }

        /// <summary>
        /// Gets or sets the client tenant ID.
        /// </summary>
        [FromHeader(Name = ClientTenantIdHeaderName)]
        public Guid? ClientTenantId { get; set; }

        /// <summary>
        /// Gets or sets the identity URL.
        /// </summary>
        [FromHeader(Name = IdentityUrlHeaderName)]
        public string IdentityUrl { get; set; }

        /// <summary>
        /// Gets or sets the identity principal ID.
        /// </summary>
        [FromHeader(Name = IdentityPrincipalIdHeaderName)]
        public Guid? IdentityPrincipalId { get; set; }
    }
}
