// <copyright file="MockComputeProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Mocks
{
    /// <summary>
    /// 
    /// </summary>
    public class MockComputeProvider : IComputeProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockComputeProvider"/> class.
        /// </summary>
        public MockComputeProvider()
        {
            Random = new Random();
            StartTracking = new ConcurrentDictionary<string, StartTrackingState>();
        }

        private ConcurrentDictionary<string, StartTrackingState> StartTracking { get; }

        private Random Random { get; }

        /// <inheritdoc/>
        public async Task<VirtualMachineProviderStartComputeResult> StartComputeAsync(VirtualMachineProviderStartComputeInput input, string continuationToken = null)
        {
            await Task.Delay(Random.Next(100, 1000));

            // Simulate initial call
            if (string.IsNullOrEmpty(continuationToken))
            {
                continuationToken = Guid.NewGuid().ToString();
                StartTracking.TryAdd(continuationToken, new StartTrackingState { TargetCount = Random.Next(2, 3), Counter = 0 });

                var initialResult = new VirtualMachineProviderStartComputeResult()
                {
                    ContinuationToken = continuationToken,
                    RetryAfter = TimeSpan.FromSeconds(1),
                    Status = "Starting",
                };

                return initialResult;
            }

            // Similate polling calls
            StartTracking.TryGetValue(continuationToken, out var stateTracker);
            stateTracker.Counter++;

            var continueResult = new VirtualMachineProviderStartComputeResult()
            {
                ContinuationToken = continuationToken,
                RetryAfter = TimeSpan.FromSeconds(1),
                Status = "Running",
            };
            if (stateTracker.Counter == stateTracker.TargetCount)
            {
                continueResult.RetryAfter = default(TimeSpan);
                continueResult.ContinuationToken = null;
                continueResult.Status = "Finished";
            }

            return continueResult;
        }

        /// <inheritdoc/>
        public Task<VirtualMachineProviderCreateResult> CreateAsync(VirtualMachineProviderCreateInput input, string continuationToken = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<VirtualMachineProviderDeleteResult> DeleteAsync(VirtualMachineProviderDeleteInput input, string continuationToken = null)
        {
            throw new NotImplementedException();
        }

        private class StartTrackingState
        {
            public int TargetCount { get; set; }

            public int Counter { get; set; }
        }
    }
}
