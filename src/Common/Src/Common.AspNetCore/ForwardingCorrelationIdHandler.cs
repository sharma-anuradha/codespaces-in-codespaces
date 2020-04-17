// <copyright file="ForwardingCorrelationIdHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.AspNetCore.Http;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore
{
    /// <summary>
    /// Forward the request correlation id header.
    /// </summary>
    public class ForwardingCorrelationIdHandler : DelegatingHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ForwardingCorrelationIdHandler"/> class.
        /// </summary>
        /// <param name="innerHandler">The inner handler.</param>
        public ForwardingCorrelationIdHandler(
            HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            ContextAccessor = new HttpContextAccessor();
        }

        private IHttpContextAccessor ContextAccessor { get; }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var correlationId = ContextAccessor?.HttpContext?.GetCorrelationId();
            if (!string.IsNullOrEmpty(correlationId))
            {
                request.Headers.Add(HttpConstants.CorrelationIdHeader, correlationId);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
