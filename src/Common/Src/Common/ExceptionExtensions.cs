// <copyright file="ExceptionExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// <see cref="Exception"/> extensions.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Get a property from <see cref="Exception.Data"/>.
        /// </summary>
        /// <typeparam name="T">The expected data type.</typeparam>
        /// <param name="ex">The exception.</param>
        /// <param name="name">The data property name.</param>
        /// <returns>The value, or default it not defined.</returns>
        public static T GetDataValue<T>(this Exception ex, string name)
        {
            Requires.NotNull(ex, nameof(ex));
            Requires.NotNull(name, nameof(name));

            if (ex.Data[name] is T value)
            {
                return value;
            }

            return default;
        }

        /// <summary>
        /// Set a property in <see cref="Exception.Data"/>.
        /// </summary>
        /// <typeparam name="T">The data type.</typeparam>
        /// <param name="ex">The exception.</param>
        /// <param name="name">The data property name.</param>
        /// <param name="value">The data value.</param>
        public static void SetDataValue<T>(this Exception ex, string name, T value)
        {
            Requires.NotNull(ex, nameof(ex));
            Requires.NotNull(name, nameof(name));
            ex.Data[name] = value;
        }
    }
}
