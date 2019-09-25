// <copyright file="ListExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    ///
    /// </summary>
    public static class ListExtensions
    {
        private static Random rng = new Random();

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns>Returns shuffled list.</returns>
        public static IList<T> Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            return list;
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
