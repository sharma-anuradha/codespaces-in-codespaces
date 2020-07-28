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
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
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

            return dictionary.TryGetValue(key, out var value) ? value : defaultValueCallback();
        }

        /// <summary>
        /// Gets value casted to specified type.
        /// </summary>
        /// <typeparam name="TValue">Value type.</typeparam>
        /// <param name="dictionary">Target dictionary.</param>
        /// <param name="key">Target key.</param>
        /// <param name="value">Found value or default.</param>
        /// <returns>True if the key is found and can be casted to the given type, false otherwise.</returns>
        public static bool TryGetValue<TValue>(
            this IDictionary<string, object> dictionary,
            string key,
            out TValue value)
        {
            value = default;

            if (!dictionary.TryGetValue(key, out var rawValue))
            {
                return false;
            }

            if (rawValue is TValue tValue)
            {
                value = tValue;
                return true;
            }

            return false;
        }
    }
}
