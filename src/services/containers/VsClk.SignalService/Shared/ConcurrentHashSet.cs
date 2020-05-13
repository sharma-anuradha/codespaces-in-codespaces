// <copyright file="ConcurrentHashSet.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.VsCloudKernel.SignalService.Common
{
    /// <summary>
    /// Helper class for a concurrent hashset.
    /// </summary>
    /// <typeparam name="T">Type of key being used.</typeparam>
    internal class ConcurrentHashSet<T>
    {
        private readonly ConcurrentDictionary<T, byte> dictionary;

        public ConcurrentHashSet()
        {
            this.dictionary = new ConcurrentDictionary<T, byte>();
        }

        public ConcurrentHashSet(IEnumerable<T> values)
            : this()
        {
            AddValues(values);
        }

        public ConcurrentHashSet(IEqualityComparer<T> comparer)
        {
            this.dictionary = new ConcurrentDictionary<T, byte>(comparer);
        }

        public int Count => this.dictionary.Count;

        public ICollection<T> Values => this.dictionary.Keys;

        public void Add(T value)
        {
            this.dictionary.AddOrUpdate(value, 0, (k, v) => 0);
        }

        public void AddValues(IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }

        public bool TryRemove(T value)
        {
            return this.dictionary.TryRemove(value, out var b);
        }

        public void RemoveValues(IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                TryRemove(value);
            }
        }

        public bool Contains(T value)
        {
            return this.dictionary.TryGetValue(value, out var b);
        }

        public void Clear()
        {
            this.dictionary.Clear();
        }
    }
}
