// <copyright file="NgrokHttpClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Ngrok
{
    /// <summary>
    /// Builds NgrokHttpClient.
    /// </summary>
    public class NgrokHttpClient
    {
        private const string ListTunnelsPath = "/api/tunnels";
        private const string GetTunnelPathFormat = "/api/tunnels/{0}";
        private const string StartTunnelPath = "/api/tunnels";
        private const string StopTunnelPathFormat = "/api/tunnels/{0}";

        /// <summary>
        /// Initializes a new instance of the <see cref="NgrokHttpClient"/> class.
        /// </summary>
        /// <param name="client">The HttpClient to be used for handling Ngrok requests.</param>
        public NgrokHttpClient(HttpClient client)
        {
            client.BaseAddress = new Uri("http://localhost:4040");
            client.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Client = client;
        }

        /// <summary>
        /// Gets the base HttpClient.
        /// </summary>
        public HttpClient Client { get; }

        /// <summary>
        /// Lists the Ngrok tunnels currently running.
        /// </summary>
        /// <param name="cancellationToken">Notification that the Task should stop.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IEnumerable<Tunnel>> ListTunnelsAsync(CancellationToken cancellationToken = default)
        {
            var response = await Client.GetAsync(ListTunnelsPath);
            await ThrowIfError(response);

            var responseString = await response.Content.ReadAsStringAsync();
            var listTunnelResponse = JsonConvert.DeserializeObject<ListTunnelsResponse>(responseString);
            return listTunnelResponse.Tunnels;
        }

        /// <summary>
        /// Start a new Ngrok tunnel session.
        /// </summary>
        /// <param name="request">The parameters needed to start the tunnel.</param>
        /// <param name="cancellationToken">Notification that the Task should stop.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<Tunnel> StartTunnelAsync(StartTunnelRequest request, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response;
            using (var content = new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json"))
            {
                response = await Client.PostAsync(StartTunnelPath, content);
            }

            await ThrowIfError(response);

            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Tunnel>(responseString);
        }

        /// <summary>
        /// Gets the Ngrok tunnel, if it's running.
        /// </summary>
        /// <param name="name">The name of the tunnel.</param>
        /// <param name="cancellationToken">Notification that the Task should stop.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<Tunnel> GetTunnelAsync(string name, CancellationToken cancellationToken = default)
        {
            var response = await Client.GetAsync(string.Format(GetTunnelPathFormat, name));
            await ThrowIfError(response);

            var responseString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Tunnel>(responseString);
        }

        /// <summary>
        /// Stops an Ngrok tunnel from running.
        /// </summary>
        /// <param name="name">The name of the tunnel to be turned off.</param>
        /// <param name="cancellationToken">Notification that the Task should stop.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task StopTunnelAsync(string name, CancellationToken cancellationToken = default)
        {
            var response = await Client.DeleteAsync(string.Format(StopTunnelPathFormat, name));
            await ThrowIfError(response);
        }

        /// <summary>
        /// Checks if Ngrok is running on the local system by pinging the API.
        /// </summary>
        /// <param name="cancellationToken">Notification that the Task should stop.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<bool> IsNgrokRunningAsync(CancellationToken cancellationToken = default)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://localhost:4040");
                try
                {
                    var response = await client.GetAsync(ListTunnelsPath, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    return true;
                }
                catch (HttpRequestException)
                {
                    // If we catch an HttpRequestException, we can assume that it's not running.
                    return false;
                }
                catch (Exception)
                {
                    // If it's any other kind of exception, we might want to check to see what's going on...
                    return false;
                }
            }
        }

        private async Task ThrowIfError(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(responseString);
                throw new NgrokApiException(errorResponse);
            }
        }
    }
}
