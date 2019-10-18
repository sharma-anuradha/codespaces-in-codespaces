using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.HttpClient.Common
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
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Add("VsSaaS-Correlation-Id", Guid.NewGuid().ToString());

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
