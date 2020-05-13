// <copyright file="ConcurrentHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;

namespace Microsoft.VsCloudKernel.SignalService.Common
{
    /// <summary>
    /// Concurrent Helpers.
    /// </summary>
    internal static class ConcurrentHelpers
    {
        public static void AddOrUpdate(
            this ConcurrentDictionary<string, ConcurrentHashSet<string>> map,
            string key,
            string[] values)
        {
            AddOrUpdate(map, key, (valuesSet) => valuesSet.AddValues(values));
        }

        public static void AddOrUpdate<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> map,
            TKey key,
            Action<TValue> valueCallback)
            where TValue : class, new()
        {
            map.AddOrUpdate(
                key,
                (k) =>
                {
                    var value = new TValue();
                    valueCallback(value);
                    return value;
                }, (k, v) =>
                {
                    valueCallback(v);
                    return v;
                });
        }

        public static void RemoveValues(
            this ConcurrentDictionary<string, ConcurrentHashSet<string>> map,
            string key,
            string[] values)
        {
            ConcurrentHashSet<string> valuesSet;
            if (map.TryGetValue(key, out valuesSet))
            {
                valuesSet.RemoveValues(values);
            }
        }

#pragma warning disable SA1314 // Type parameter names should begin with T
        public static V AddOrUpdate<T, U, V>(
            this ConcurrentDictionary<T, U> dictionary,
            T key,
            Func<T, V> addValueFactory,
            Func<T, V, V> updateValueFactory)
            where U : Lazy<V>
        {
            U lazy = dictionary.AddOrUpdate(
                key,
                (U)new Lazy<V>(() => addValueFactory(key)),
                (k, oldValue) => (U)new Lazy<V>(() => updateValueFactory(k, oldValue.Value)));
            return lazy.Value;
        }
    }
#pragma warning restore SA1314 // Type parameter names should begin with T
}
