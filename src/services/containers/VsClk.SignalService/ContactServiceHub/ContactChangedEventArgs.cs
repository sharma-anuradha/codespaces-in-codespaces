// <copyright file="ContactChangedEventArgs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Event to send when the contact has been changed.
    /// </summary>
    internal class ContactChangedEventArgs : EventArgs
    {
        internal ContactChangedEventArgs(
            string connectionId,
            ConnectionProperties properties,
            ContactUpdateType changeType)
        {
            ConectionId = connectionId;
            Properties = properties;
            ChangeType = changeType;
        }

        public string ConectionId { get; }

        public ConnectionProperties Properties { get; }

        public ContactUpdateType ChangeType { get; }
    }
}
