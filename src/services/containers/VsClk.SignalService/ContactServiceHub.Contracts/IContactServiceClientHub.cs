// <copyright file="IContactServiceClientHub.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Contract for Hub presence client notifications
    /// </summary>
    public interface IContactServiceClientHub
    {
        /// <summary>
        /// Invoked when the hub notify on a Contact if some of the subscribed properties have changed
        /// </summary>
        /// <param name="contact">The contact id who's properties are being changed</param>
        /// <param name="notifyProperties">Dictionary of properties being changed</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateValuesAsync(ContactReference contact, Dictionary<string, object> notifyProperties, CancellationToken cancellationToken);

        /// <summary>
        /// Invoked when the hub route a message to a particular client
        /// </summary>
        /// <param name="targetContact">The recipient contact who will receive the message</param>
        /// <param name="fromContact">The contact who originally send the message </param>
        /// <param name="type">Type of the message being send</param>
        /// <param name="body">Body of the message</param>
        /// <returns></returns>
        Task ReceiveMessageAsync(ContactReference targetContact, ContactReference fromContact, string type, object body);

        /// <summary>
        /// Invoked when the hub report a new connection is being resgistered on a contact
        /// </summary>
        /// <param name="contact"></param>
        /// <param name="changeType"></param>
        /// <returns></returns>
        Task ConnectionChangedAsync(ContactReference contact, ConnectionChangeType changeType);
    }
}
