// <copyright file="IContactBackplaneManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// IBackplaneManager interface to manage multiple provider registration
    /// </summary>
    public interface IContactBackplaneManager : IContactBackplaneProviderBase, IBackplaneManagerBase<IContactBackplaneProvider, ContactBackplaneProviderSupportLevel, ContactServiceMetrics>
    {
        /// <summary>
        /// Event to report contact changed notification from a provider
        /// </summary>
        event OnContactChangedAsync ContactChangedAsync;

        /// <summary>
        /// Event to report a received message from a provider
        /// </summary>
        event OnMessageReceivedAsync MessageReceivedAsync;
    }

    /// <summary>
    /// Class to expose supported capability level of the contact provider.
    /// </summary>
    public class ContactBackplaneProviderSupportLevel : BackplaneProviderSupportLevelBase
    {
        public int? GetContact { get; set; }

        public int? GetContacts { get; set; }

        public int? SendMessage { get; set; }

        public int? UpdateContact { get; set; }
    }
}
