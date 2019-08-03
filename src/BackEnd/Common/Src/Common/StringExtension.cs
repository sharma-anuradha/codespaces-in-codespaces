// <copyright file="StringExtension.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

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
    }
}
