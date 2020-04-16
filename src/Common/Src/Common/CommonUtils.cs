// <copyright file="CommonUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
    }
}
