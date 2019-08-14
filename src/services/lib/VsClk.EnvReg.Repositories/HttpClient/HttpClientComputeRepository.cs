using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using VsClk.EnvReg.Models.DataStore.Compute;
using VsClk.EnvReg.Repositories.Support.HttpClient;

namespace VsClk.EnvReg.Repositories.HttpClient
{
    public class HttpClientComputeRepository : IComputeRepository
    {
        public HttpClientComputeRepository(IHttpClientProvider httpClientProvider)
        {
            HttpClientProvider = httpClientProvider;
        }

        private IHttpClientProvider HttpClientProvider { get; }

        public async Task<List<ComputeTargetResponse>> GetTargetsAsync()
        {
            var response = await HttpClientProvider.ComputeServiceClient.GetAsync("/computeTargets");

            await response.ThrowIfFailedAsync();

            var targets = await response.Content.ReadAsAsync<List<ComputeTargetResponse>>();

            return targets;
        }

        public async Task<ComputeResourceResponse> AddResourceAsync(string computeTargetId, ComputeServiceRequest computeServiceRequest)
        {
            var response = await HttpClientProvider.ComputeServiceClient.PostAsync(
                $"/computeTargets/{computeTargetId}/compute",
                computeServiceRequest,
                new JsonMediaTypeFormatter());

            await response.ThrowIfFailedAsync();

            var targets = await response.Content.ReadAsAsync<ComputeResourceResponse>();

            return targets;
        }

        public async Task DeleteResourceAsync(string connectionComputeTargetId, string connectionComputeId)
        {
            var response = await HttpClientProvider.ComputeServiceClient.DeleteAsync(
                $"/computeTargets/{connectionComputeTargetId}/compute/{connectionComputeId}");

            await response.ThrowIfFailedAsync();
        }

        public Task<ComputeResourceResponse> RefreshResourceAsync(string computeTargetId, string computeId, ComputeServiceRequest computeServiceRequest)
        {
            throw new NotImplementedException();
        }
    }
}
