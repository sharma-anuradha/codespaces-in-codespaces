// <copyright file="PathExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore.Extensions
{
    /// <summary>
    /// Extensions for Path.
    /// </summary>
    internal static class PathExtensions
    {
        /// <summary>
        /// Get a fully qualified path for a file on the PATH variable. Include .exe if searching for an exe.
        /// </summary>
        /// <param name="searchApp">The file/path to search for.</param>
        /// <returns>The full path.</returns>
        public static string GetFullPathFromEnvPath(string searchApp)
        {
            var enviromentPath = Environment.GetEnvironmentVariable("PATH");
            if (enviromentPath == null)
            {
                return null;
            }

            var paths = enviromentPath.Split(Path.DirectorySeparatorChar);
            var exePath = paths.Select(x => Path.Combine(x, searchApp))
                .FirstOrDefault(File.Exists);
            return exePath;
        }
    }
}
