// <copyright file="ContactUpdateType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Type of change when updating a contact
    /// </summary>
    public enum ContactUpdateType
    {
        /// <summary>
        /// Default none option
        /// </summary>
        None,

        /// <summary>
        /// When a contact is being registered
        /// </summary>
        Registration,

        /// <summary>
        /// When the contact is being updated
        /// </summary>
        UpdateProperties,

        /// <summary>
        /// When the contact is being unregistered
        /// </summary>
        Unregister,
    }
}