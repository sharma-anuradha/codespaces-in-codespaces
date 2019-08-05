// <copyright file="ConnectionChangedEventArgs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// Event to notify when a connection is beign added or removed on a contact.
    /// </summary>
    public class ConnectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionChangedEventArgs"/> class.
        /// </summary>
        /// <param name="contact">The contact that changed.</param>
        /// <param name="changeType">Type of change.</param>
        internal ConnectionChangedEventArgs(ContactReference contact, ConnectionChangeType changeType)
        {
            Contact = contact;
            ChangeType = changeType;
        }

        /// <summary>
        /// Gets contact that changed.
        /// </summary>
        public ContactReference Contact { get; }

        /// <summary>
        /// Gets the connection change type.
        /// </summary>
        public ConnectionChangeType ChangeType { get; }
    }
}
