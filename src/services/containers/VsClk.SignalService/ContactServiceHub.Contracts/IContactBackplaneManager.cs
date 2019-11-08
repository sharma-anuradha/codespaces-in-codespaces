using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.SignalService
{
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// Const for the backplane manager implementation
    /// </summary>
    public static class BackplaneManagerConst
    {
        /// <summary>
        /// Number of secons to push new service metrics.
        /// </summary>
        public const int UpdateMetricsSecs = 45;

        /// <summary>
        /// Number of seconds to consider a service to be 'stale'
        /// </summary>
        public const int StaleServiceSeconds = UpdateMetricsSecs * 3;
    }

    /// <summary>
    /// IBackplaneManager interface to manage multiple provider registration
    /// </summary>
    public interface IContactBackplaneManager
    {
        /// <summary>
        /// Event to report contact changed notification from a provider
        /// </summary>
        event OnContactChangedAsync ContactChangedAsync;

        /// <summary>
        /// Event to report a received message from a provider
        /// </summary>
        event OnMessageReceivedAsync MessageReceivedAsync;

        /// <summary>
        /// Run a long running task to update metrics and purge
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        Task RunAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Dispose of the backplane manager
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DisposeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets or Sets the metrics factory callback.
        /// </summary>
        Func<((string ServiceId, string Stamp), ContactServiceMetrics)> MetricsFactory { get; set; }

        /// <summary>
        /// Gets the backplane providers.
        /// </summary>
        IReadOnlyCollection<IContactBackplaneProvider> BackplaneProviders { get; }

        /// <summary>
        /// Register a new provider
        /// </summary>
        /// <param name="backplaneProvider"></param>
        void RegisterProvider(IContactBackplaneProvider backplaneProvider);

        /// <summary>
        /// Update metrics reported by a contact service
        /// </summary>
        /// <param name="serviceInfo">Info on the service being reported</param>
        /// <param name="metrics">Metrics to report</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateBackplaneMetrics(
            (string ServiceId, string Stamp) serviceInfo,
            ContactServiceMetrics metrics,
            CancellationToken cancellationToken);

        /// <summary>
        /// Start tracking a data change that later will be purged
        /// </summary>
        /// <param name="dataChanged"></param>
        /// <returns></returns>
        bool TrackDataChanged(DataChanged dataChanged);

        /// <summary>
        /// Return the contacts that match a set of property conditions
        /// </summary>
        /// <param name="matchProperties"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Dictionary<string, ContactDataInfo>> GetContactsDataAsync(Dictionary<string, object> matchProperties, CancellationToken cancellationToken);

        /// <summary>
        /// Return the contact data
        /// </summary>
        /// <param name="contactId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken);

        /// <summary>
        /// Update a contact into all the hosted providers
        /// </summary>
        /// <param name="contactDataChanged"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken);

        /// <summary>
        /// Send a message using all the backplane providers
        /// </summary>
        /// <param name="serviceId"></param>
        /// <param name="messageData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SendMessageAsync(string serviceId, MessageData messageData, CancellationToken cancellationToken);
    }
}
