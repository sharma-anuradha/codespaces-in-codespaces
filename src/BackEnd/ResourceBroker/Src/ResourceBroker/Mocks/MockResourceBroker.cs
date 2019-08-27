// <copyright file="MockResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
        public Task<AllocateResult> AllocateAsync(AllocateInput input, IDiagnosticsLogger logger)
        {
            var now = DateTime.UtcNow;
            var location = (AzureLocation)Enum.Parse(typeof(AzureLocation), input.Location);
            return Task.FromResult(new AllocateResult
            {
                Created = now,
                Location = input.Location,
                ResourceId = new ResourceId(input.Type, Guid.NewGuid(), MockSubscriptionId, MockResourceGroup, location),
                SkuName = input.SkuName,
                Type = input.Type,
            });
        }

        /// <inheritdoc/>
        public Task<bool> DeallocateAsync(string resourceIdToken, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
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
                return new EnvironmentStartResult
                {
                    ContinuationToken = input.ComputeResourceId,
                    ResourceId = input.ComputeResourceId,
                    RetryAfter = TimeSpan.FromMinutes(1),
                    Status = OperationState.NotStarted,
                };
            });

            // Mock the VM callback.
            _ = task.ContinueWith(async t =>
            {
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));

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
