// <copyright file="UnauthorizedUtil.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Utilities for throwing <see cref="UnauthorizedAccessException"/>.
    /// </summary>
    public static class UnauthorizedUtil
    {
        /// <summary>
        /// Throw <see cref="UnauthorizedAccessException"/> if <paramref name="value"/> is null.
        /// </summary>
        /// <param name="value">The value to test.</param>
        public static void IsRequired(object value)
        {
            if (value == null)
            {
                throw new UnauthorizedAccessException();
            }
        }

        /// <summary>
        /// Throw <see cref="UnauthorizedAccessException"/> if <paramref name="value"/> is null or empty.
        /// </summary>
        /// <param name="value">The value to test.</param>
        public static void IsRequired(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new UnauthorizedAccessException();
            }
        }

        /// <summary>
        /// Throw <see cref="UnauthorizedAccessException"/> if <paramref name="value"/> is false.
        /// </summary>
        /// <param name="value">The value to test.</param>
        public static void IsTrue(bool value)
        {
            if (!value)
            {
                throw new UnauthorizedAccessException();
            }
        }
    }
}
