// <copyright file="PrivacyCommandExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.PrivacyServices.CommandFeed.Client;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PCFAgent
{
    /// <summary>
    /// Privacy Command Extensions.
    /// </summary>
    public static class PrivacyCommandExtensions
    {
        private const string AttemptNumberKey = "AttemptNumber";
        private const string AffectedRowCountKey = "AffectedRowCount";

        /// <summary>
        /// Mark a new attempt of this command, with the affected row count.
        /// </summary>
        /// <param name="command">IPrivacyCommand object.</param>
        /// <param name="affectedRowCount">AffectedRowCount.</param>
        public static void MarkNewAttemptWithAffectedRowCount(this IPrivacyCommand command, int affectedRowCount)
        {
            var attemptNumber = GetAttemptNumber(command) + 1;
            var stateDictionary = new Dictionary<string, int>
            {
                { AttemptNumberKey, attemptNumber },
                { AffectedRowCountKey, affectedRowCount },
            };

            command.AgentState = JsonConvert.SerializeObject(stateDictionary);
        }

        /// <summary>
        /// Attempt number for this command.
        /// </summary>
        /// <param name="command">IPrivacyCommand object.</param>
        /// <returns>Attempt number.</returns>
        public static int GetAttemptNumber(this IPrivacyCommand command) => GetValueFromStateDictionary<int>(command, AttemptNumberKey);

        /// <summary>
        /// AffectedRowCount for this command.
        /// </summary>
        /// <param name="command">IPrivacyCommand object.</param>
        /// <returns>AffectedRowCount.</returns>
        public static int GetAffectedRowCount(this IPrivacyCommand command) => GetValueFromStateDictionary<int>(command, AffectedRowCountKey);

        private static T GetValueFromStateDictionary<T>(IPrivacyCommand command, string key)
        {
            try
            {
                var stateDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(command.AgentState);
                if (stateDictionary.TryGetValue(key, out object value))
                {
                    return (T)value;
                }
            }
            catch
            {
                // Swallow.
            }

            return default;
        }
    }
}
