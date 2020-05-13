// <copyright file="DictionaryHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VsCloudKernel.SignalService.Common
{
    /// <summary>
    /// IDictionary helpers.
    /// </summary>
    internal static class DictionaryHelpers
    {
        public static string ConvertToString(this IDictionary<string, object> properties, IDataFormatProvider provider)
        {
            if (properties == null)
            {
                return "(null)";
            }

            return string.Join("|", properties.Select(i => $"{i.Key}={FormatValue(provider, i.Key, i.Value)}"));
        }

        public static bool MatchProperties(this IDictionary<string, object> matchingPropertes, IDictionary<string, object> allProperties)
        {
            foreach (var kvp in matchingPropertes)
            {
                if (!(allProperties.TryGetValue(kvp.Key, out var value) && value?.Equals(kvp.Value) == true))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool EqualsProperties(this IDictionary<string, object> properties, IDictionary<string, object> otherProperties)
        {
            if (properties.Count != otherProperties.Count)
            {
                return false;
            }

            return MatchProperties(properties, otherProperties);
        }

        public static T TryGetProperty<T>(this Dictionary<string, object> properties, string propertyName, T defaultValue = default(T))
        {
            if (properties.TryGetValue(propertyName, out var value) && value != null)
            {
                if (!(value is IConvertible))
                {
                    value = value.ToString();
                }

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    throw new InvalidCastException($"Failed to cast value for property:{propertyName} and type:{value.GetType().FullName}");
                }
            }

            return defaultValue;
        }

        public static void AddOrUpdate<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            Action<TValue> valueCallback,
            object lockHelper = null)
            where TValue : class, new()
        {
            Assumes.NotNull(dictionary);

            Action addOrUpdate = () =>
            {
                TValue value;
                if (!dictionary.TryGetValue(key, out value))
                {
                    value = new TValue();
                    dictionary.Add(key, value);
                }

                valueCallback(value);
            };

            if (lockHelper != null)
            {
                lock (lockHelper)
                {
                    addOrUpdate();
                }
            }
            else
            {
                addOrUpdate();
            }
        }

        public static Dictionary<TKey, TValue> Clone<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return dictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private static string FormatValue(IDataFormatProvider provider, string propertyName, object value)
        {
            if (value is byte[] buffer)
            {
                return $"data-length:{buffer.Length}";
            }

            var format = provider?.GetPropertyFormat(propertyName);
            return string.Format(provider, string.IsNullOrEmpty(format) ? "{0}" : $"{{0:{format}}}", value);
        }
    }
}
