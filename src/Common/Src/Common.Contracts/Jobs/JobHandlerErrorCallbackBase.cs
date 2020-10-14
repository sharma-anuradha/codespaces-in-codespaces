// <copyright file="JobHandlerErrorCallbackBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Base class that implements IJobHandlerErrorCallback
    /// </summary>
    public abstract class JobHandlerErrorCallbackBase : IJobHandlerErrorCallback
    {
        /// <inheritdoc/>
        public Task<JobCompletedStatus> HandleJobErrorAsync(IJob job, Exception error, JobCompletedStatus status, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            if (DidRetryException(error, out var retryTimeout))
            {
                logger.FluentAddValue("RetryJobHandlerError", this.GetType().Name)
                    .FluentAddValue("RetryException", error.GetType().Name)
                    .FluentAddValue("RetryTimeout", retryTimeout);

                job.VisibilityTimeout = retryTimeout;
                return Task.FromResult(status | JobCompletedStatus.Retry);
            }

            return Task.FromResult(JobCompletedStatus.None);
        }

        protected static T ToException<T>(Exception error)
            where T : Exception
        {
            if (error is T)
            {
                return (T)error;
            }
            else if (error is AggregateException aggregateException)
            {
                if (aggregateException.InnerException is T)
                {
                    return (T)aggregateException.InnerException;
                }

                return (T)aggregateException.InnerExceptions.FirstOrDefault(e => e is T);
            }

            return null;
        }

        protected abstract bool DidRetryException(Exception error, out TimeSpan retryTimeout);
    }
}
