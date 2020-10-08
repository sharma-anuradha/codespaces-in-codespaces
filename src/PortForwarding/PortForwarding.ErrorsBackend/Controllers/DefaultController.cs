// <copyright file="DefaultController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.ErrorsBackend.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.ErrorsBackend.Controllers
{
    public class DefaultController : Controller
    {
        // FormatHeader name of the header used to extract the format
        private const string FormatHeader = "X-Format";

        // CodeHeader name of the header used as source of the HTTP status code to return
        private const string CodeHeader = "X-Code";

        // ContentType name of the header that defines the format of the reply
        private const string ContentType = "Content-Type";

        // OriginalURI name of the header with the original URL from NGINX
        private const string OriginalURI = "X-Original-URI";

        // Namespace name of the header that contains information about the Ingress namespace
        private const string Namespace = "X-Namespace";

        // IngressName name of the header that contains the matched Ingress
        private const string IngressName = "X-Ingress-Name";

        // ServiceName name of the header that contains the matched Service in the Ingress
        private const string ServiceName = "X-Service-Name";

        // ServicePort name of the header that contains the matched Service port in the Ingress
        private const string ServicePort = "X-Service-Port";

        // RequestId is a unique ID that identifies the request - same as for backend service
        private const string RequestId = "X-Request-ID";

        // Authentication cookie for Port Forwarding
        private const string PFCookieName = "__Host-vso-pf";

        public DefaultController(IStringLocalizer<DefaultController> localizer)
        {
            Localizer = localizer;
        }

        private IStringLocalizer<DefaultController> Localizer { get; }

        [HttpOperationalScope("error_page")]
        public IActionResult Index(
            [FromHeader(Name = FormatHeader)] string formatHeader,
            [FromHeader(Name = CodeHeader)] HttpStatusCode statusCode,
            [FromHeader(Name = ContentType)] string contentType,
            [FromHeader(Name = OriginalURI)] string originalURI,
            [FromHeader(Name = Namespace)] string serviceNamespace,
            [FromHeader(Name = IngressName)] string ingressName,
            [FromHeader(Name = ServiceName)] string serviceName,
            [FromHeader(Name = ServicePort)] string servicePort,
            [FromHeader(Name = RequestId)] string requestId,
            [FromServices] IDiagnosticsLogger logger)
        {
            logger
                .FluentAddValue("FormatHeader", formatHeader)
                .FluentAddValue("CodeHeader", statusCode)
                .FluentAddValue("ContentType", contentType)
                .FluentAddValue("OriginalURI", originalURI)
                .FluentAddValue("Namespace", serviceNamespace)
                .FluentAddValue("IngressName", ingressName)
                .FluentAddValue("ServiceName", serviceName)
                .FluentAddValue("ServicePort", servicePort)
                .FluentAddValue("RequestId", requestId);

            logger.LogInfo("error_page_params");

            if (statusCode == 0)
            {
                statusCode = HttpStatusCode.NotFound;
            }

            var statusText = Localizer[ReasonPhrases.GetReasonPhrase(Convert.ToInt32(statusCode))];

            ViewBag.Title = Localizer.GetString("{0} · GitHub", statusText);
            ViewBag.Favicon = "/favicon.png";

            var errorDetail = GetErrorDetailMessage(statusCode);

            // Clean PF authentication cookie (it can be responsible of some error, we want users to re-authenticate)
            Response.Cookies.Append(PFCookieName, "expired", new CookieOptions { Expires = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(2)), HttpOnly = true, Secure = true });

            var model = new ErrorPageDetails
            {
                RequestId = requestId,
                StatusCode = statusCode,
                StatusText = statusText,
                ErrorDetail = Localizer[errorDetail],
                DocumentationUri = new Uri("https://aka.ms/codespaces-troubleshooting"),
                StatusPageUri = new Uri("https://aka.ms/codespaces-status-page"),
            };

            return View(model);

            static string GetErrorDetailMessage(HttpStatusCode statusCode) => statusCode switch
            {
                HttpStatusCode.BadRequest => string.Empty,
                HttpStatusCode.Unauthorized => string.Empty,
                HttpStatusCode.Forbidden => string.Empty,
                HttpStatusCode.NotFound => string.Empty,
                HttpStatusCode.TooManyRequests => string.Empty,
                HttpStatusCode.InternalServerError => "Something went wrong, one of your services may have crashed.",
                HttpStatusCode.BadGateway => "Make sure your port is open and ready to receive incoming HTTP traffic.",
                HttpStatusCode.ServiceUnavailable => "This can happen if the Codespace has been shut down.",
                HttpStatusCode.GatewayTimeout => "The port forwarding timed out while trying to create a connection.",
                _ => string.Empty,
            };
        }
    }
}
