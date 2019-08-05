// <copyright file="ReceiveMessageEventArgs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    /// <summary>
    /// EventArgs to notify when receiving a message from the signalR hub.
    /// </summary>
    public class ReceiveMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageEventArgs"/> class.
        /// </summary>
        /// <param name="targetContact">Target contact to send the message.</param>
        /// <param name="fromContact">Which contact send the data.</param>
        /// <param name="messageType">The message type.</param>
        /// <param name="body">Body of the message.</param>
        internal ReceiveMessageEventArgs(
            ContactReference targetContact,
            ContactReference fromContact,
            string messageType,
            object body)
        {
            TargetContact = targetContact;
            FromContact = fromContact;
            Type = messageType;
            Body = body;
        }

        /// <summary>
        /// Gets target contact to send the message.
        /// </summary>
        public ContactReference TargetContact { get; }

        /// <summary>
        /// Gets which contact send the data.
        /// </summary>
        public ContactReference FromContact { get; }

        /// <summary>
        /// Gets the message type.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Gets body of the message.
        /// </summary>
        public object Body { get; }
    }
}
