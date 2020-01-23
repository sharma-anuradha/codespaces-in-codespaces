// <copyright file="BaseMockResourceProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Mocks
{
    /// <summary>
    /// Base mock resource provider.
    /// </summary>
    public class BaseMockResourceProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseMockResourceProvider"/> class.
        /// </summary>
        public BaseMockResourceProvider()
        {
            Random = new Random();
            StartTracking = new ConcurrentDictionary<string, StartTrackingState>();
        }

        private ConcurrentDictionary<string, StartTrackingState> StartTracking { get; }

        private Random Random { get; }

        /// <summary>
        /// Runner the simulates the continution process that the providers go through.
        /// </summary>
        /// <typeparam name="TI">Input type needed by the provider.</typeparam>
        /// <typeparam name="TR">Result type returned by the provider.</typeparam>
        /// <param name="input">Required input.</param>
        /// <param name="logger">Logger that is used.</param>
        /// <returns>Provider result.</returns>
        protected async Task<TR> RunAsync<TI, TR>(TI input, IDiagnosticsLogger logger)
            where TI : ContinuationInput
            where TR : ContinuationResult, new()
        {
            await Task.Delay(Random.Next(100, 1000));
            var continuationToken = input.ContinuationToken;

            // Simulate initial call
            if (string.IsNullOrEmpty(continuationToken))
            {
                continuationToken = Guid.NewGuid().ToString();
                StartTracking.TryAdd(continuationToken, new StartTrackingState { TargetCount = Random.Next(2, 3), Counter = 0 });

                var initialResult = new TR()
                {
                    RetryAfter = TimeSpan.FromSeconds(1),
                    Status = OperationState.InProgress,
                    NextInput = input.BuildNextInput(continuationToken),
                };

                return initialResult;
            }

            // Similate polling calls
            StartTracking.TryGetValue(continuationToken, out var stateTracker);
            stateTracker.Counter++;

            var continueResult = new TR()
            {
                RetryAfter = TimeSpan.FromSeconds(1),
                Status = OperationState.InProgress,
                NextInput = input.BuildNextInput(continuationToken),
            };
            if (stateTracker.Counter == stateTracker.TargetCount)
            {
                continueResult.RetryAfter = default(TimeSpan);
                continueResult.NextInput = null;
                continueResult.Status = OperationState.Succeeded;
            }

            return continueResult;
        }

        private class StartTrackingState
        {
            public int TargetCount { get; set; }

            public int Counter { get; set; }
        }
    }
}
