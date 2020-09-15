// <copyright file="CosmosClientJobHandlerError.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Azure.Cosmos;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// CosmosException support for job handler error callbacks
    /// </summary>
    public class CosmosClientJobHandlerError : JobHandlerErrorCallbackBase
    {
        private const int TooManyRequests = 429;

        protected override bool DidRetryException(Exception error, out TimeSpan retryTimeout)
        {
            var cosmosException = ToException<CosmosException>(error);
            if ((int)cosmosException?.StatusCode == TooManyRequests)
            {
                retryTimeout = cosmosException.RetryAfter.HasValue ? cosmosException.RetryAfter.Value : TimeSpan.FromSeconds(5);
                return true;
            }

            retryTimeout = default;
            return false;
        }
    }
}
