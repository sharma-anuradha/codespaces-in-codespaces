// <copyright file="UpdatePropertiesEventArgs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// EventArgs to notify when properties on a contact changed on the signalR hub
    /// </summary>
    public class UpdatePropertiesEventArgs : EventArgs
    {
        internal UpdatePropertiesEventArgs(ContactReference contact, Dictionary<string, object> properties, string targetConnectionId)
        {
            Contact = contact;
            Properties = properties;
            TargetConnectionId = targetConnectionId;
        }

        /// <summary>
        /// Gets the contact who change its properties.
        /// </summary>
        public ContactReference Contact { get; }

        /// <summary>
        /// Gets actual properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets a target connection id
        /// </summary>
        public string TargetConnectionId { get; }
    }
}
