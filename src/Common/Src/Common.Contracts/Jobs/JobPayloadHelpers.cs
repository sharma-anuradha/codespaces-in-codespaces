// <copyright file="JobPayloadHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VsSaaS.Diagnostics;

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

        /// <summary>
        /// Create a compatible logger properties from an existing dictionary.
        /// </summary>
        /// <typeparam name="T">Type of the value type.</typeparam>
        /// <param name="loggerProperties">Existing logger properties.</param>
        /// <returns>A logger properties dictionary to add into a payload.</returns>
        public static Dictionary<string, object> CreateLoggerProperties<T>(this IDictionary<string, T> loggerProperties)
        {
            Requires.NotNull(loggerProperties, nameof(loggerProperties));

            return loggerProperties.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        }

        /// <summary>
        /// Add properties to a payload.
        /// </summary>
        /// <typeparam name="T">Type of job payload.</typeparam>
        /// <param name="jobPayload">The job payload instance.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="keys">Set of keys to include.</param>
        public static T WithLoggerProperties<T>(this T jobPayload, IDiagnosticsLogger logger, params string[] keys)
            where T : JobPayload
        {
            Requires.NotNull(jobPayload, nameof(jobPayload));
            Requires.NotNull(logger, nameof(logger));
            Requires.NotNull(keys, nameof(keys));

            if (logger is IDiagnosticsLoggerContext loggerContext)
            {
                var logValues = keys.Select(key =>
                {
                    if (loggerContext.TryGetValue(key, out var value))
                    {
                        return new KeyValuePair<string, object>(key, value);
                    }

                    return default;
                }).Where(i => i.Key != null);
                MergeLoggerProperties(jobPayload, logValues);
            }

            return jobPayload;
        }

        /// <summary>
        /// Initialize a continuation job payload.
        /// </summary>
        /// <typeparam name="T">Type of the payload.</typeparam>
        /// <param name="jobContinuationPayload">The job continuation payload instance.</param>
        /// <returns>Reference to the job payload.</returns>
        public static T InitializeContinuationPayload<T>(this T jobContinuationPayload)
            where T : ContinuationJobPayload
        {
            jobContinuationPayload.UtcCreated = DateTime.UtcNow;
            jobContinuationPayload.LoggerProperties.Add("CorrelationId", Guid.NewGuid());
            return jobContinuationPayload;
        }
    }
}
