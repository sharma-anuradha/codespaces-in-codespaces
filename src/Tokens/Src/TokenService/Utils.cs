// <copyright file="Utils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.TokenService
{
    /// <summary>
    /// Utility functions used in the token service.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Tests if two strings (URIs) are equal aside from optional trailing slashes on either.
        /// </summary>
        /// <param name="str1">First string to compare.</param>
        /// <param name="str2">Second string to compare.</param>
        /// <returns>True if the strings are equal aside from optional trailing slashes.</returns>
        public static bool EqualsIgnoringTrailingSlash(string str1, string str2)
        {
            if (str1.EndsWith("/", StringComparison.Ordinal) &&
                !str2.EndsWith("/", StringComparison.Ordinal))
            {
                return
                    str1.Length - 1 == str2.Length &&
                    string.Compare(str1, 0, str2, 0, str2.Length, StringComparison.Ordinal) == 0;
            }

            if (!str1.EndsWith("/", StringComparison.Ordinal) &&
                str2.EndsWith("/", StringComparison.Ordinal))
            {
                return
                    str1.Length == str2.Length - 1 &&
                    string.Compare(str1, 0, str2, 0, str1.Length, StringComparison.Ordinal) == 0;
            }

            return str1.Equals(str2, StringComparison.Ordinal);
        }
    }
}
