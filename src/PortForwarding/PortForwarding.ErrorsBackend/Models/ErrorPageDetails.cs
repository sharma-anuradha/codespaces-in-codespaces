// <copyright file="ErrorPageDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.ErrorsBackend.Models
{
    public class ErrorPageDetails
    {
        public HttpStatusCode StatusCode { get; set; }

        public string StatusText { get; set; } = default!;

        public string ErrorDetail { get; set; } = default!;

        public Uri DocumentationUri { get; set; } = default!;

        public Uri StatusPageUri { get; set; } = default!;

        public string RequestId { get; set; } = default!;
    }
}
