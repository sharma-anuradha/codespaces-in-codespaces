// <copyright file="MockResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Mocks
{
    /// <summary>
    /// The mock Resource Broker.
    /// </summary>
    public class MockResourceBroker : IResourceBroker
    {
        private static readonly Guid MockSubscriptionId = Guid.NewGuid();
        private static readonly string MockResourceGroup = "MockResourceGroup";

        /// <inheritdoc/>
        public Task<IEnumerable<AllocateResult>> AllocateAsync(
            IEnumerable<AllocateInput> inputs, IDiagnosticsLogger logger)
        {
            var now = DateTime.UtcNow;
            var results = new List<AllocateResult>();
            foreach (var input in inputs)
            {
                var resourceId = Guid.NewGuid();
                results.Add(new AllocateResult
                {
                    Created = now,
                    Location = input.Location,
                    Id = resourceId,
                    AzureResourceInfo = new AzureResourceInfo(MockSubscriptionId, MockResourceGroup, resourceId.ToString()),
                    SkuName = input.SkuName,
                    Type = input.Type,
                });
            }

            return Task.FromResult((IEnumerable<AllocateResult>)results);
        }

        /// <inheritdoc/>
        public Task<AllocateResult> AllocateAsync(AllocateInput input, IDiagnosticsLogger logger)
        {
            var now = DateTime.UtcNow;
            var resourceId = Guid.NewGuid();
            return Task.FromResult(new AllocateResult
            {
                Created = now,
                Location = input.Location,
                Id = resourceId,
                AzureResourceInfo = new AzureResourceInfo(MockSubscriptionId, MockResourceGroup, resourceId.ToString()),
                SkuName = input.SkuName,
                Type = input.Type,
            });
        }

        public Task<CleanupResult> CleanupAsync(CleanupInput input, IDiagnosticsLogger logger)
        {
            return Task.FromResult(new CleanupResult { Successful = true });
        }

        /// <inheritdoc/>
        public Task<DeallocateResult> DeallocateAsync(DeallocateInput input, IDiagnosticsLogger logger)
        {
            return Task.FromResult(new DeallocateResult { Successful = true });
        }

        /// <inheritdoc/>
        public async Task<EnvironmentStartResult> StartComputeAsync(EnvironmentStartInput input, IDiagnosticsLogger logger)
        {
            // TODO: get these from shared Constants.
            const string SessionCallbackVaraible = "SESSION_CALLBACK";
            const string SessionTokenVariable = "SESSION_TOKEN";
            const string SessionIdVariable = "SESSION_ID";

            var callbackUri = input.EnvironmentVariables[SessionCallbackVaraible];
            var sessionToken = input.EnvironmentVariables[SessionTokenVariable];
            var sessionId = input.EnvironmentVariables[SessionIdVariable];
            string sessionPath = "Mock-Path";

            var task = Task.Run(() =>
            {
                return new EnvironmentStartResult { Successful = true };
            });

            // Mock the VM callback.
            _ = task.ContinueWith(async t =>
            {
                await Task.Delay(TimeSpan.FromSeconds(2));

                var callback = new EnvironmentRegistrationCallbackBody
                {
                    Payload = new EnvironmetnRegistrationCallbackPayloadBody
                    {
                        SessionId = sessionId,
                        SessionPath = sessionPath,
                    },
                    Type = "connectioninfo",
                };

                using (var client = new HttpClient())
                {
                    logger.FluentAddValue("CallbackUri", callbackUri)
                        .LogInfo("mock_resource_broker_callback");

                    // Content
                    var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(callback), System.Text.Encoding.UTF8, "application/json");

                    // Post
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {sessionToken}");
                    var result = await client.PostAsync(callbackUri, content);
                    if (!result.IsSuccessStatusCode)
                    {
                        logger.LogErrorWithDetail("mock_resource_broker_callback_failed", result.StatusCode.ToString());
                    }
                }
            });

            return await task;
        }
    }
}
