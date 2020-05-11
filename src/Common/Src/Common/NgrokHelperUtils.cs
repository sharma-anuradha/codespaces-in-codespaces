// <copyright file="NgrokHelperUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Helper Utilities for dealing with Ngrok.
    /// </summary>
    public class NgrokHelperUtils
    {
        /// <summary>
        /// Returns the local ngrok hostname if it's running. Else throws an exception.
        /// </summary>
        /// <returns>Ngrok Hostname.</returns>
        public static async Task<string> GetLocalNgrokHostname()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    // Get the running tunnels from Ngrok...
                    var httpResponse = await client.GetAsync("http://localhost:4040/api/tunnels");
                    var result = JsonConvert.DeserializeObject<dynamic>(await httpResponse.Content.ReadAsStringAsync());
                    return new Uri((string)result.tunnels[0].public_url).Host;
                }
                catch (HttpRequestException hrex)
                {
                    throw new HttpRequestException($"Couldn't access Ngrok, is it running?", hrex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unknown error occured accessing Ngrok Tunnels API.", ex);
                }
            }
        }
    }
}
