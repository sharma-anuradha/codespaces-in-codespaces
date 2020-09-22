// <copyright file="JobSchedulePayloadFactoryHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Extension methods for <see cref="IJobSchedulePayloadFactory"/> and related operations.
    /// </summary>
    public static class JobSchedulePayloadFactoryHelpers
    {
        public static Task AddAllPayloadsAsync(this OnPayloadCreatedDelegate onPayloadCreated, IEnumerable<(JobPayload, JobPayloadOptions)> payloadItems)
        {
            return Task.WhenAll(payloadItems.Select((payloadItem) => onPayloadCreated(payloadItem.Item1, payloadItem.Item2)));
        }

        public static Task AddAllPayloadsAsync<T>(this OnPayloadCreatedDelegate onPayloadCreated, IEnumerable<T> items, Func<T, (JobPayload, JobPayloadOptions)> transform)
        {
            return onPayloadCreated.AddAllPayloadsAsync(items.Select(transform));
        }

        public static Task AddAllPayloadsAsync(this OnPayloadCreatedDelegate onPayloadCreated, IEnumerable<JobPayload> payloads)
        {
            return onPayloadCreated.AddAllPayloadsAsync(payloads, (payload) => (payload, default(JobPayloadOptions)));
        }

        public static Task AddAllPayloadsAsync<T>(this OnPayloadCreatedDelegate onPayloadCreated, IEnumerable<T> items, Func<T, JobPayload> transform)
        {
            return onPayloadCreated.AddAllPayloadsAsync(items.Select(transform));
        }
    }
}
