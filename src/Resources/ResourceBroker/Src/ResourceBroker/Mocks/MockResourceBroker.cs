// <copyright file="MockResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;

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
        public Task<IEnumerable<AllocateResult>> AllocateAsync(Guid environmentId, IEnumerable<AllocateInput> inputs, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties)
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
        public Task<AllocateResult> AllocateAsync(Guid environmentId, AllocateInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties)
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

        /// <inheritdoc/>
        public async Task<bool> StartAsync(Guid environmentId, StartAction action, IEnumerable<StartInput> input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            if (action == StartAction.StartCompute)
            {
                if (input != null || !input.Any() || input.Count() != 2)
                {
                    throw new NotSupportedException("Input does not have the required number of resources.");
                }

                var computeResource = input.ElementAt(0);
                var storageResource = input.ElementAt(1);

                // TODO: get these from shared Constants.
                const string SessionCallbackVaraible = "SESSION_CALLBACK";
                const string SessionTokenVariable = "SESSION_TOKEN";
                const string SessionIdVariable = "SESSION_ID";

                var callbackUri = computeResource.Variables[SessionCallbackVaraible];
                var sessionToken = computeResource.Variables[SessionTokenVariable];
                var sessionId = computeResource.Variables[SessionIdVariable];
                var sessionPath = "Mock-Path";

                var task = Task.Run(() =>
                {
                    return true;
                });

                // Mock the VM callback.
                _ = task.ContinueWith(async t =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));

                    var callback = new EnvironmentRegistrationCallbackBody
                    {
                        Payload = new EnvironmentRegistrationCallbackPayloadBody
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
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc/>
        public Task<bool> StartAsync(Guid environmentId, StartAction action, StartInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> SuspendAsync(Guid environmentId, IEnumerable<SuspendInput> input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> SuspendAsync(Guid environmentId, SuspendInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(Guid environmentId, IEnumerable<DeleteInput> inputs, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(Guid environmentId, DeleteInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<StatusResult>> StatusAsync(Guid environmentId, IEnumerable<StatusInput> input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<StatusResult> StatusAsync(Guid environmentId, StatusInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> ProcessHeartbeatAsync(Guid id, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null)
        {
            return Task.FromResult(true);
        }
    }
}