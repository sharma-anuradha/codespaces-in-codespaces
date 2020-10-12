// <copyright file="DocumentClientJobHandlerError.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Azure.Documents;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// DocumentClientException support for job handler error callbacks
    /// </summary>
    public class DocumentClientJobHandlerError : JobHandlerErrorCallbackBase
    {
        private const int TooManyRequests = 429;

        protected override bool DidRetryException(Exception error, out TimeSpan retryTimeout)
        {
            var documentClientException = ToException<DocumentClientException>(error);
            if (documentClientException != null && (int?)documentClientException.StatusCode == TooManyRequests)
            {
                retryTimeout = documentClientException.RetryAfter;
                return true;
            }

            retryTimeout = default;
            return false;
        }
    }
}
