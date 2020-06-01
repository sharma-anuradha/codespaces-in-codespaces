// <copyright file="IContactBackplaneProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;
using ContactDataInfo = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>>;

namespace Microsoft.VsCloudKernel.SignalService
{
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

    /// <summary>
    /// Class to describe a contact change.
    /// </summary>
    /// <typeparam name="T">Type of data contained on this data change type.</typeparam>
    public sealed class ContactDataChanged<T> : DataChanged
        where T : class
    {
        public ContactDataChanged(string changeId, string serviceId, string connectionId, string contactId, ContactUpdateType changeType, T data)
            : base(changeId)
        {
            Requires.NotNullOrEmpty(serviceId, nameof(serviceId));
            Requires.NotNullOrEmpty(connectionId, nameof(connectionId));
            Requires.NotNullOrEmpty(contactId, nameof(contactId));

            ServiceId = serviceId;
            ConnectionId = connectionId;
            ContactId = contactId;
            ChangeType = changeType;
            Data = Requires.NotNull(data, nameof(data));
        }

        public ContactDataChanged<TData> Clone<TData>(TData data)
            where TData : class
        {
            return new ContactDataChanged<TData>(ChangeId, ServiceId, ConnectionId, ContactId, ChangeType, data);
        }

        public string ServiceId { get; }

        public string ConnectionId { get; }

        public string ContactId { get; }

        public ContactUpdateType ChangeType { get; }

        public T Data { get; }
    }

    /// <summary>
    /// Instance to refer to a contact data changed.
    /// </summary>
    /// <typeparam name="T">The supported type of the data. It could be ContactDataInfo or ConnectionProperties types.</typeparam>
    public sealed class ContactDataChangedRef<T>
        where T : class
    {
        private readonly Lazy<ContactDataChanged<ContactDataInfo>> lazyAsContactDataInfo;
        private readonly Lazy<ContactDataChanged<ConnectionProperties>> lazyAsConnectionProperties;

        public ContactDataChangedRef(ContactDataChanged<T> contactDataChanged)
        {
            DataChanged = contactDataChanged;
            IsConnectionProperties = typeof(T) == typeof(ConnectionProperties);

            this.lazyAsContactDataInfo = new Lazy<ContactDataChanged<ContactDataInfo>>(() => (ContactDataChanged<ContactDataInfo>)(contactDataChanged as object));
            this.lazyAsConnectionProperties = new Lazy<ContactDataChanged<ConnectionProperties>>(
                () => IsConnectionProperties ? (ContactDataChanged<ConnectionProperties>)(contactDataChanged as object) : contactDataChanged.Clone(ContactDataInfo.GetConnectionProperties()));
        }

        public bool IsConnectionProperties { get; }

        public ContactDataChanged<ContactDataInfo> ContactDataInfo => this.lazyAsContactDataInfo.Value;

        public ContactDataChanged<ConnectionProperties> ConnectionProperties => this.lazyAsConnectionProperties.Value;

        public ContactDataChanged<T> DataChanged { get; set; }
    }

    /// <summary>
    /// The message data entity.
    /// </summary>
    public class MessageData : DataChanged
    {
        public MessageData(
            string changeId,
            string serviceId,
            ContactReference fromContact,
            ContactReference targetContact,
            string type,
            object body)
            : base(changeId)
        {
            ServiceId = serviceId;
            FromContact = fromContact;
            TargetContact = targetContact;
            Type = type;
            Body = body;
        }

        /// <summary>
        /// Gets the service identifier who originate the change.
        /// </summary>
        public string ServiceId { get; }

        /// <summary>
        /// The contact who want to send the message.
        /// </summary>
        public ContactReference FromContact { get; }

        /// <summary>
        /// The target contact to send the message.
        /// </summary>
        public ContactReference TargetContact { get; }

        /// <summary>
        /// Type of the message.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Body content of the message.
        /// </summary>
        public object Body { get; }
    }

    /// <summary>
    /// Backplane data provider interface.
    /// </summary>
    public interface IContactBackplaneDataProvider
    {
        /// <summary>
        /// Return matching contacts
        /// </summary>
        /// <param name="matchProperties">The match properties to look for</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Array of contact data entities that match the criteria</returns>
        Task<Dictionary<string, ContactDataInfo>[]> GetContactsDataAsync(Dictionary<string, object>[] matchProperties, CancellationToken cancellationToken);

        /// <summary>
        /// Get the contact data info
        /// </summary>
        /// <param name="contactId">The contact id to query</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The contact data entity if it exists, null otherwise</returns>
        Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken);

        /// <summary>
        /// Update the contact properties.
        /// </summary>
        /// <param name="contactDataChanged">The contact data info that changed</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Completion task.</returns>
        Task UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Invoked when a remote contact has changed
    /// </summary>
    /// <param name="contactDataChanged">The contact data info that changed</param>
    /// <param name="affectedProperties">Affected properties that impact this change</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public delegate Task OnContactChangedAsync(
            ContactDataChanged<ContactDataInfo> contactDataChanged,
            string[] affectedProperties,
            CancellationToken cancellationToken);

    /// <summary>
    /// Invoked when a message was send from a remote service
    /// </summary>
    /// <param name="messageData">The message data entity</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public delegate Task OnMessageReceivedAsync(
        MessageData messageData,
        CancellationToken cancellationToken);

    /// <summary>
    /// Contacts backplane provider base.
    /// </summary>
    public interface IContactBackplaneProviderBase : IContactBackplaneDataProvider
    {
        /// <summary>
        /// Send a message using the backplane provider.
        /// </summary>
        /// <param name="messageData">The message data entity.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Task completion.</returns>
        Task SendMessageAsync(MessageData messageData, CancellationToken cancellationToken);

        /// <summary>
        /// Update a consolidated contact.
        /// </summary>
        /// <param name="contactDataChanged">Contact data change entity.</param>
        /// <param name="contactDataInfoValues">The updated values of the contact info.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task UpdateContactDataInfoAsync(
            ContactDataChanged<ConnectionProperties> contactDataChanged,
            (ContactDataInfo NewValue, ContactDataInfo OldValue) contactDataInfoValues,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Interface to surface a backplane provider.
    /// </summary>
    public interface IContactBackplaneProvider : IContactBackplaneProviderBase, IBackplaneProviderBase<ContactServiceMetrics>
    {
        OnContactChangedAsync ContactChangedAsync { get; set; }

        OnMessageReceivedAsync MessageReceivedAsync { get; set; }
    }

#pragma warning restore SA1201 // Elements should appear in the correct order
#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
}
