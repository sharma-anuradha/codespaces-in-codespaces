// <copyright file="ValidationUtil.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Validation utilities.
    /// </summary>
    public static class ValidationUtil
    {
        /// <summary>
        /// Tests whether a required value is null.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="value">The value to test.</param>
        /// <param name="name">The property name to report; optional.</param>
        /// <returns>The <paramref name="value"/>.</returns>
        /// <exception cref="ValidationException"><paramref name="value"/> is null.</exception>
        public static T IsRequired<T>(T value, string name = null)
        {
            if (value == null)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ValidationException($"Input is invalid");
                }

                throw new ValidationException($"'{name}' is required");
            }

            return value;
        }

        /// <summary>
        /// Tests whether a required string is null or empty.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <param name="name">The property name to report; optional.</param>
        /// <returns>The <paramref name="value"/>.</returns>
        /// <exception cref="ValidationException"><paramref name="value"/> is null or empty.</exception>
        public static string IsRequired(string value, string name = null)
        {
            if (string.IsNullOrEmpty(value))
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ValidationException($"Input is invalid");
                }

                throw new ValidationException($"'{name}' is required");
            }

            return value;
        }

        /// <summary>
        /// Test that a value is true.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <param name="message">The validation error message.</param>
        /// <exception cref="ValidationException"><paramref name="value"/> is false.</exception>
        public static void IsTrue(bool value, string message = null)
        {
            if (!value)
            {
                if (string.IsNullOrEmpty(message))
                {
                    throw new ValidationException($"Input is invalid");
                }

                throw new ValidationException(message);
            }
        }
    }
}
