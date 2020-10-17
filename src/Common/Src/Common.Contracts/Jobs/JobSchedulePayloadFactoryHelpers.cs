// <copyright file="JobSchedulePayloadFactoryHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Extension methods for <see cref="IJobSchedulePayloadFactory"/> and related operations.
    /// </summary>
    public static class JobSchedulePayloadFactoryHelpers
    {
        public static Task AddPayloadAsync(this OnPayloadsCreatedDelegateAsync onPayloadCreated, JobPayload jobPayload, JobPayloadOptions jobPayloadOptions, CancellationToken cancellationToken = default)
        {
            return onPayloadCreated(new[] { (jobPayload, jobPayloadOptions) }, cancellationToken);
        }

        public static Task AddAllPayloadsAsync<T>(this OnPayloadsCreatedDelegateAsync onPayloadCreated, IEnumerable<T> items, Func<T, (JobPayload, JobPayloadOptions)> transform, CancellationToken cancellationToken = default)
        {
            return onPayloadCreated(items.Select(transform), cancellationToken);
        }

        public static Task AddAllPayloadsAsync(this OnPayloadsCreatedDelegateAsync onPayloadCreated, IEnumerable<JobPayload> payloads, CancellationToken cancellationToken = default)
        {
            return onPayloadCreated.AddAllPayloadsAsync(payloads, (payload) => (payload, default(JobPayloadOptions)), cancellationToken);
        }

        public static Task AddAllPayloadsAsync<T>(this OnPayloadsCreatedDelegateAsync onPayloadCreated, IEnumerable<T> items, Func<T, JobPayload> transform, CancellationToken cancellationToken = default)
        {
            return onPayloadCreated.AddAllPayloadsAsync(items.Select(transform), cancellationToken);
        }
    }
}
