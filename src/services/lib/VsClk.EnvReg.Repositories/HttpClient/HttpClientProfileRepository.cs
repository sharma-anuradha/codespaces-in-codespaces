using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using VsClk.EnvReg.Models.DataStore;
using VsClk.EnvReg.Repositories.Support.HttpClient;

namespace VsClk.EnvReg.Repositories.HttpClient
{
    public class HttpClientProfileRepository : IProfileRepository
    {
        public HttpClientProfileRepository(
            ICurrentUserProvider currentUserProvider,
            IHttpClientProvider httpClientProvider)
        {
            CurrentUserProvider = currentUserProvider;
            HttpClientProvider = httpClientProvider;
        }

        private ICurrentUserProvider CurrentUserProvider { get; }

        private IHttpClientProvider HttpClientProvider { get; }

        public async Task<Profile> GetCurrentUserProfileAsync(IDiagnosticsLogger logger)
        {
            var response = await HttpClientProvider.ProfileServiceClient.GetAsync("profile?scope=programs");
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return null;
            }

            await response.ThrowIfFailedAsync();

            var profile = await response.Content.ReadAsAsync<Profile>();

            return profile;
        }
    }
}
