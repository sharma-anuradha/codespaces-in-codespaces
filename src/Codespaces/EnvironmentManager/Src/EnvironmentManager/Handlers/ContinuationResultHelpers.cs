// <copyright file="ContinuationResultHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// Continuation result helpers.
    /// </summary>
    internal static class ContinuationResultHelpers
    {
#pragma warning disable SA1600 // Elements should be documented
        public static ContinuationResult ReturnInProgress(object payload, TimeSpan? retryAfter = null)
        {
            var result = new ContinuationResult()
            {
                Status = OperationState.InProgress,
            };
            if (payload is ContinuationInput continuationInput)
            {
                result.NextInput = continuationInput;
            }

            if (retryAfter.HasValue)
            {
                result.RetryAfter = retryAfter.Value;
            }

            return result;
        }

        public static ContinuationResult ReturnSucceeded()
        {
            return new ContinuationResult { Status = OperationState.Succeeded };
        }

        public static ContinuationResult ReturnFailed(string errorReason)
        {
            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = errorReason };
        }

#pragma warning restore SA1600 // Elements should be documented
    }
}
