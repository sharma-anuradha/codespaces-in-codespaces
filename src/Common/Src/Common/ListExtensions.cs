// <copyright file="ListExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// List extension methods.
    /// </summary>
    public static class ListExtensions
    {
        private static Random rng = new Random();

        /// <summary>
        /// Shuffles the items in the IEnumerable.
        /// </summary>
        /// <typeparam name="T">Generic type.</typeparam>
        /// <param name="list">Target list.</param>
        /// <returns>Returns shuffled list.</returns>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list)
        {
            var l = list.ToList();
            var n = l.Count;
            while (n > 1)
            {
                n--;
                var k = rng.Next(n + 1);
                T value = l[k];
                l[k] = l[n];
                l[n] = value;
            }

            return l;
        }

        /// <summary>
        /// Randomly pick an item in the list.
        /// </summary>
        /// <typeparam name="T">Type of element.</typeparam>
        /// <param name="input">Target input.</param>
        /// <returns>Sinlge radonly picked item.</returns>
        public static T RandomOrDefault<T>(this IEnumerable<T> input)
        {
            if (!input.Any())
            {
                return default(T);
            }

            return input.ElementAt(rng.Next(input.Count()));
        }
    }
}
