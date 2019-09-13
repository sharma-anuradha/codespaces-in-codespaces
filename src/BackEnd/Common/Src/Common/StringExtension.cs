// <copyright file="StringExtension.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    public static class StringExtension
    {
        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static string ToBase64Encoded(this string value)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string GetDeterministicHashCode(this string value)
        {
            var sb = new StringBuilder();
            using (var hash = SHA1.Create())
            {
                var result = hash.ComputeHash(Encoding.UTF8.GetBytes(value));
                foreach (var b in result)
                {
                    _ = sb.Append(b.ToString("x2"));
                }
            }

            return sb.ToString();
        }
    }
}
