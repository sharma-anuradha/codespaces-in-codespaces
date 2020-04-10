// <copyright file="VsoClientType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics
{
    /// <summary>
    /// Indicates a public Azure Geography.
    /// </summary>
    public enum VsoClientType
    {
        /// <summary>
        /// Visual Studio.
        /// </summary>
        VisualStudio,

        /// <summary>
        /// Visual Studio Code.
        /// </summary>
        VisualStudioCode,

        /// <summary>
        /// Visual Studio Web Portal.
        /// </summary>
        WebPortal,

        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown = 0,
    }
}
