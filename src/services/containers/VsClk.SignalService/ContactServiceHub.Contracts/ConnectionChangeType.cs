// <copyright file="ConnectionChangeType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Connection changed type.
    /// </summary>
    public enum ConnectionChangeType
    {
        /// <summary>
        /// No action.
        /// </summary>
        None,

        /// <summary>
        /// When a connection is added
        /// </summary>
        Added,

        /// <summary>
        /// When a connection is removed.
        /// </summary>
        Removed,
    }
}
