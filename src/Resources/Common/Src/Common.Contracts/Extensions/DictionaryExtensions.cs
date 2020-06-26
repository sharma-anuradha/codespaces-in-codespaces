// <copyright file="DictionaryExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts.Extensions
{
    /// <summary>
    /// Dictionary extensions.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Compares two dictionaries and returns true if they are equal.
        /// </summary>
        /// <typeparam name="T1">Type 1 key.</typeparam>
        /// <typeparam name="T2">Type 2 value.</typeparam>
        /// <param name="first">First dictionary.</param>
        /// <param name="second">Second dictionary.</param>
        /// <returns>True if they are equal.</returns>
        public static bool DictionaryEquals<T1, T2>(
            this IDictionary<T1, T2> first,
            IDictionary<T1, T2> second)
        {
            if (first == second)
            {
                return true;
            }

            if (first == default || second == default)
            {
                return false;
            }

            if (first.Count != second.Count)
            {
                return false;
            }

            return first.All(item =>
            {
                return
                    second.TryGetValue(item.Key, out var secondValue) &&
                    item.Value.Equals(secondValue);
            });
        }
    }
}
