// <copyright file="LoggingBaseNameAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

/// <summary>
/// Specify the logging base name for a class.
/// </summary>
/// <remarks>
/// TODO: move to VS SaaS SDK Diagnostics.
/// </remarks>
namespace Microsoft.VsSaaS.Diagnostics.Extensions
{
    /// <summary>
    /// Overrides the logging base name used by <see cref="LoggingExtensions"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class LoggingBaseNameAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingBaseNameAttribute"/> class.
        /// </summary>
        /// <param name="loggingBaseName">The logging base name.</param>
        public LoggingBaseNameAttribute(string loggingBaseName)
        {
            LoggingBaseName = loggingBaseName;
        }

        /// <summary>
        /// Gets the logging base name.
        /// </summary>
        public string LoggingBaseName { get; }
    }
}
