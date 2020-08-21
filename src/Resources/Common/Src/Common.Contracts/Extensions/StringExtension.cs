// <copyright file="StringExtension.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// String extensions.
    /// </summary>
    public static class StringExtension
    {
        /// <summary>
        /// Converts string to enum of provided type.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="value">string value.</param>
        /// <returns>Typed enum.</returns>
        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        /// <summary>
        /// Encodes string to base 64.
        /// </summary>
        /// <param name="value">Text to encode.</param>
        /// <returns>Encoded text.</returns>
        public static string ToBase64Encoded(this string value)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        /// Generates SHA1 hash of provided string.
        /// </summary>
        /// <param name="value">String to hash.</param>
        /// <returns>SHA1 hash.</returns>
        public static string GetDeterministicHashCode(this string value)
        {
            var sb = new StringBuilder();
#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
            using (var hash = SHA1.Create())
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms
            {
                var result = hash.ComputeHash(Encoding.UTF8.GetBytes(value));
                foreach (var b in result)
                {
                    _ = sb.Append(b.ToString("x2"));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Trucate a string.
        /// </summary>
        /// <param name="value">String to truncate.</param>
        /// <param name="maxChars">Maximum number of characters to be retained.</param>
        /// <returns>Truncated string.</returns>
        public static string Truncate(this string value, int maxChars)
        {
            if (value.Length <= maxChars)
            {
                return value;
            }

            return $"{value.Substring(0, maxChars)}...";
        }
    }
}
