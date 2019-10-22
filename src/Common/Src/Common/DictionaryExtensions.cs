// <copyright file="DictionaryExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Dictionary Extensions.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Gets value or returns default if not found.
        /// </summary>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <param name="dictionary">Target dictionary.</param>
        /// <param name="key">Target key.</param>
        /// <param name="defaultValue">Target default value.</param>
        /// <returns>Found value or default.</returns>
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue = default)
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }

        /// <summary>
        /// Gets value or returns default if not found.
        /// </summary>
        /// <typeparam name="TKey">Key type.</typeparam>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <param name="dictionary">Target dictionary.</param>
        /// <param name="key">Target key.</param>
        /// <param name="defaultValueCallback">Target default value callback.</param>
        /// <returns>Found value or default.</returns>
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TValue> defaultValueCallback)
        {
            Requires.NotNull(defaultValueCallback, nameof(defaultValueCallback));

            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValueCallback();
        }
    }
}
