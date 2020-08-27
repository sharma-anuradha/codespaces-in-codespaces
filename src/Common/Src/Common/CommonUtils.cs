// <copyright file="CommonUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Common utilities and helpers.
    /// </summary>
    public class CommonUtils
    {
        /// <summary>
        /// Read string content from an embedded resource.
        /// </summary>
        /// <param name="fullyQualifiedResourceName">Fully qualified embedded resource name.</param>
        /// <returns>Embedded resource content.</returns>
        public static string GetEmbeddedResourceContent(string fullyQualifiedResourceName)
        {
            var assembly = Assembly.GetCallingAssembly();
            using (var stream = assembly.GetManifestResourceStream(fullyQualifiedResourceName))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Validate string value is not null or empty.
        /// </summary>
        /// <param name="value">value.</param>
        /// <param name="propertyName">property name.</param>
        /// <param name="className">class name.</param>
        /// <returns>validated result.</returns>
        public static string NotNullOrWhiteSpace(string value, string propertyName, string className)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"The property {nameof(className)}.{propertyName} is required.");
            }

            return value;
        }

        /// <summary>
        /// Converts a camel case string to pascal case.
        /// </summary>
        /// <param name="value">value.</param>
        /// <returns>result.</returns>
        public static string CamelToPascalCase(string value)
        {
            Requires.NotNull(value, nameof(value));

            return value.Length == 0 ? string.Empty :
                value.Substring(0, 1).ToUpperInvariant() + value.Substring(1);
        }
    }
}
