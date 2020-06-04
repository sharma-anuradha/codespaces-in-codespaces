// <copyright file="SecretFilterUtil.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider
{
    /// <summary>
    /// Utility class for filtering a list of secrets.
    /// </summary>
    public static class SecretFilterUtil
    {
        /// <summary>
        /// Compute applicable secrets by applying filters.
        /// </summary>
        /// <param name="secrets">List of secrets.</param>
        /// <param name="filterDataCollection">List of filter data.</param>
        /// <returns>Filtered list of secrets.</returns>
        public static IEnumerable<UserSecret> ComputeApplicableSecrets(
            IEnumerable<UserSecret> secrets,
            IEnumerable<SecretFilterData> filterDataCollection)
        {
            var weightedApplicableSecrets = new HashSet<(UserSecret secret, int weight)>();
            foreach (var secret in secrets.OrderByDescending(x => x.LastModified))
            {
                // Secret with highest weight is choosen in case of multiple matching secrets with same name.
                // In case of a tie, the most recently updated secret wins (as it is sorted desc).
                if (TryGetFilterationWeight(secret, filterDataCollection, out var weight))
                {
                    var existingSecretEntry = weightedApplicableSecrets.FirstOrDefault(x => x.secret.SecretName == secret.SecretName && x.secret.Type == secret.Type);
                    if (existingSecretEntry != default)
                    {
                        if (existingSecretEntry.weight > weight)
                        {
                            continue;
                        }

                        weightedApplicableSecrets.Remove(existingSecretEntry);
                    }

                    weightedApplicableSecrets.Add((secret, weight));
                }
            }

            return weightedApplicableSecrets.Select(x => x.secret);
        }

        /// <summary>
        /// Filteration weight indicates the granularity of the filters on the matched secret.
        /// Returns true if there is a match.
        /// Called would choose the secret with highest weight in case of multiple matching secrets with same name.
        /// </summary>
        private static bool TryGetFilterationWeight(
            UserSecret secret,
            IEnumerable<SecretFilterData> filterDataCollection,
            out int weight)
        {
            // Default weight
            weight = 0;

            // Return true if the secret has no filters
            if (secret.Filters == null || !secret.Filters.Any())
            {
                return true;
            }

            foreach (var secretFilter in secret.Filters)
            {
                var filterData = filterDataCollection?.SingleOrDefault(filterData => filterData.Type == secretFilter.Type);
                if (filterData == default)
                {
                    return false;
                }

                if (!MatchWildCard(filterData.Data, secretFilter.Value))
                {
                    return false;
                }

                // Each matching filter adds a weight of 1000 + the number of qualifying filter characters.
                // Deeper the filter granularity, higher the weight.
                weight += 1000 + RemoveWildCards(secretFilter.Value).Length;
            }

            return true;
        }

        /// <summary>
        /// Match a string using wildcard pattern with '*' and '?'.
        /// </summary>
        private static bool MatchWildCard(string input, string pattern, bool ignoreCase = true)
        {
            var regexPattern = "^" +
                                Regex.Escape(pattern).
                                      Replace("\\*", ".*").
                                      Replace("\\?", ".") +
                                "$";
            var regexOptions = RegexOptions.CultureInvariant;
            if (ignoreCase)
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            return Regex.IsMatch(input, regexPattern, regexOptions);
        }

        /// <summary>
        /// Remove wild card characters (* and ?) from a string.
        /// </summary>
        private static string RemoveWildCards(string input)
        {
            return input?.Replace("*", string.Empty).Replace("?", string.Empty);
        }
    }
}
