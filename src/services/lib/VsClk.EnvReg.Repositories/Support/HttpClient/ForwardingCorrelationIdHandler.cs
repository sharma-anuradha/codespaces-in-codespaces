using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.AspNetCore.Http;
using Microsoft.VsSaaS.Common;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VsClk.EnvReg.Repositories.Support.HttpClient
{
    public class ForwardingCorrelationIdHandler : DelegatingHandler
    {
        public ForwardingCorrelationIdHandler(
            HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            ContextAccessor = new HttpContextAccessor();
        }

        private readonly IHttpContextAccessor ContextAccessor;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var correlationId = ContextAccessor.HttpContext.GetCorrelationId();
            if (!string.IsNullOrEmpty(correlationId))
            {
                request.Headers.Add(HttpConstants.CorrelationIdHeader, correlationId);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
