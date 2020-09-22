// <copyright file="JobPayloadHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Job payload helpers.
    /// </summary>
    public static class JobPayloadHelpers
    {
        /// <summary>
        /// Merge logger properties to payload
        /// </summary>
        /// <typeparam name="T">Type of dictionary value</typeparam>
        /// <param name="jobPayload">The job payload instance.</param>
        /// <param name="logValues">Set of values to merge.</param>
        public static void MergeLoggerProperties<T>(this JobPayload jobPayload, IEnumerable<KeyValuePair<string, T>> logValues)
        {
            Requires.NotNull(jobPayload, nameof(jobPayload));
            Requires.NotNull(logValues, nameof(logValues));

            foreach (var item in logValues)
            {
                if (!jobPayload.LoggerProperties.ContainsKey(item.Key))
                {
                    jobPayload.LoggerProperties.Add(item.Key, item.Value);
                }
            }
        }
    }
}
