using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VsClk.EnvReg.Repositories.Support.HttpClient
{
    public class ForwardingBearerAuthMessageHandler : ForwardingCorrelationIdHandler
    {
        private const string AuthHeaderName = "Authorization";
        private const string AuthTokenPrefix = "Bearer ";

        public ForwardingBearerAuthMessageHandler(
            HttpMessageHandler innerHandler,
            ICurrentUserProvider currentUserProvider)
            : base(innerHandler)
        {
            CurrentUserProvider = currentUserProvider;
        }

        private ICurrentUserProvider CurrentUserProvider { get; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var authToken = CurrentUserProvider.GetBearerToken();
            if (!string.IsNullOrEmpty(authToken))
            {
                request.Headers.Add(AuthHeaderName, AuthTokenPrefix + authToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
