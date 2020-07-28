// <copyright file="AzureManagementHttpClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.Models.PrivatePreview;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.PrivatePreview
{
    /// <summary>
    /// Azure Management HttpClient.
    /// </summary>
    public class AzureManagementHttpClient
    {
        private const string ApiUrl = "https://management.azure.com/";
        private const string ListKeyEndpoint = "listKeys";

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureManagementHttpClient"/> class.
        /// </summary>
        /// <param name="accessToken">ARM token.</param>
        public AzureManagementHttpClient(string accessToken)
        {
            HttpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiUrl),
            };

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpClient.DefaultRequestHeaders.Accept?.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private HttpClient HttpClient { get; set; }

        /// <summary>
        /// Fetch database credentials.
        /// </summary>
        /// <param name="azureInfo">Details to lookup database in azure.</param>
        /// <returns>Database Credentials.</returns>
        public async Task<DatabaseCredentials> FetchDatabaseCredentials(AzureInfo azureInfo)
        {
            var listKeyApiUrl = $"/subscriptions/{azureInfo.SubscriptionId}/resourceGroups/" +
                $"{azureInfo.ResourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/" +
                $"{azureInfo.DatabaseAccount}/{ListKeyEndpoint}?api-version=2020-04-01";

            var response = await HttpClient.PostAsync(listKeyApiUrl, null);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<DatabaseCredentials>(json);
            }
            else
            {
                var message = $"{response.StatusCode} - Failed to fetch database credentials.";
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    message += " Please check if you have JITed in.";
                }

                throw new Exception(message);
            }
        }
    }
}
