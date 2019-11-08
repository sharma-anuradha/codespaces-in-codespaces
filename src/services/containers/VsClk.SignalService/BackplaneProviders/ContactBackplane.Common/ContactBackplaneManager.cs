using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// A backplane manager that can host multiple backplane providers
    /// </summary>
    public class ContactBackplaneManager : IContactBackplaneManager
    {
        private const string MethodDisposeDataChanges = nameof(ContactBackplaneManager.DisposeDataChangesAsync);
        private const string MethodUpdateBackplaneMetrics = nameof(ContactBackplaneManager.UpdateBackplaneMetrics);

        private const string TotalContactsProperty = "NumberOfContacts";
        private const string TotalConnectionsProperty = "NumberOfConnections";
        private const string MemorySizeProperty = "MemorySize";
        private const string TotalMemoryProperty = "TotalMemory";

        private readonly object backplaneProvidersLock = new object();
        private readonly List<IContactBackplaneProvider> backplaneProviders = new List<IContactBackplaneProvider>();
        private readonly Dictionary<string, (DateTime, DataChanged)> backplaneChanges = new Dictionary<string, (DateTime, DataChanged)>();
        private readonly object backplaneChangesLock = new object();

        public ContactBackplaneManager(ILogger<ContactBackplaneManager> logger)
        {
            Logger = Requires.NotNull(logger, nameof(logger));
        }

        public event OnContactChangedAsync ContactChangedAsync;

        public event OnMessageReceivedAsync MessageReceivedAsync;

        public Func<((string ServiceId, string Stamp), ContactServiceMetrics)> MetricsFactory { get; set; }

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            const int TimespanUpdateSecs = 5;

            Logger.LogDebug($"RunAsync");

            await UpdateBackplaneMetrics(stoppingToken);

            var updateMetricsCounter = new SecondsCounter(BackplaneManagerConst.UpdateMetricsSecs, TimespanUpdateSecs);
            while (true)
            {
                // update metrics if factory is defined
                if (updateMetricsCounter.Next())
                {
                    await UpdateBackplaneMetrics(stoppingToken);
                }

                // purge data changes (every 5 secs)
                await DisposeExpiredDataChangesAsync(null, stoppingToken);

                // delay
                await Task.Delay(TimeSpan.FromSeconds(TimespanUpdateSecs), stoppingToken);
            }
        }

        public async Task DisposeAsync(CancellationToken cancellationToken)
        {
            Logger.LogDebug($"Dispose");

            DataChanged[] allDataChanges = null;
            lock (this.backplaneChangesLock)
            {
                allDataChanges = this.backplaneChanges.Select(i => i.Value.Item2).ToArray();
                this.backplaneChanges.Clear();
            }

            await DisposeDataChangesAsync(allDataChanges, cancellationToken);
            // attempt to dispose all providers
            foreach (var disposable in BackplaneProviders.OfType<VisualStudio.Threading.IAsyncDisposable>())
            {
                await disposable.DisposeAsync();
            }
        }

        /// <summary>
        /// Gets the list of backplane providers, note this is a thread safe property
        /// </summary>
        public IReadOnlyCollection<IContactBackplaneProvider> BackplaneProviders
        {
            get
            {
                lock (this.backplaneProvidersLock)
                {
                    return this.backplaneProviders.ToArray();
                }
            }
        }

        private ILogger Logger { get; }

        public void RegisterProvider(IContactBackplaneProvider backplaneProvider)
        {
            Requires.NotNull(backplaneProvider, nameof(backplaneProvider));

            Logger.LogInformation($"AddBackplaneProvider type:{backplaneProvider.GetType().FullName}");
            lock (this.backplaneProvidersLock)
            {
                this.backplaneProviders.Add(backplaneProvider);
            }

            backplaneProvider.ContactChangedAsync = (contactDataChanged, affectedProperties, ct) => OnContactChangedAsync(backplaneProvider, contactDataChanged, affectedProperties, ct);
            backplaneProvider.MessageReceivedAsync = (sourceId, messageData, ct) => OnMessageReceivedAsync(backplaneProvider, sourceId, messageData, ct);
        }

        public async Task UpdateBackplaneMetrics(
            (string ServiceId, string Stamp) serviceInfo,
            ContactServiceMetrics metrics,
            CancellationToken cancellationToken)
        {
            foreach (var backplaneProvider in BackplaneProviders)
            {
                try
                {
                    await backplaneProvider.UpdateMetricsAsync(serviceInfo, metrics, cancellationToken);
                }
                catch (Exception error)
                {
                    HandleBackplaneProviderException(backplaneProvider, nameof(IContactBackplaneProvider.UpdateMetricsAsync), error);
                }
            }
        }

        public async Task UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken)
        {
            await DisposeExpiredDataChangesAsync(100, CancellationToken.None);

            foreach (var backplaneProvider in BackplaneProviders)
            {
                try
                {
                    await backplaneProvider.UpdateContactAsync(contactDataChanged, cancellationToken);
                }
                catch (Exception error)
                {
                    HandleBackplaneProviderException(backplaneProvider, nameof(IContactBackplaneProvider.UpdateContactAsync), error);
                }
            }
        }

        public async Task SendMessageAsync(
            string serviceId,
            MessageData messageData,
            CancellationToken cancellationToken)
        {
            await DisposeExpiredDataChangesAsync(100, cancellationToken);

            foreach (var backplaneProvider in GetBackPlaneProvidersByPriority())
            {
                try
                {
                    await backplaneProvider.SendMessageAsync(serviceId, messageData, cancellationToken);
                }
                catch (Exception error)
                {
                    HandleBackplaneProviderException(backplaneProvider, nameof(IContactBackplaneProvider.SendMessageAsync), error);
                }
            }
        }

        public async Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken)
        {
            foreach (var backplaneProvider in GetBackPlaneProvidersByPriority())
            {
                try
                {
                    var contactData = await backplaneProvider.GetContactDataAsync(contactId, cancellationToken);
                    if (contactData != null)
                    {
                        return contactData;
                    }
                }
                catch (Exception error)
                {
                    HandleBackplaneProviderException(backplaneProvider, nameof(IContactBackplaneProvider.GetContactDataAsync), error);
                }
            }

            return null;
        }

        public async Task<Dictionary<string, ContactDataInfo>> GetContactsDataAsync(Dictionary<string, object> matchProperties, CancellationToken cancellationToken)
        {
            foreach (var backplaneProvider in GetBackPlaneProvidersByPriority())
            {
                try
                {
                    var contacts = await backplaneProvider.GetContactsDataAsync(matchProperties, cancellationToken);
                    if (contacts?.Count > 0)
                    {
                        return contacts;
                    }
                }
                catch (Exception error)
                {
                    HandleBackplaneProviderException(backplaneProvider, nameof(IContactBackplaneProvider.GetContactsDataAsync), error);
                }
            }

            return new Dictionary<string, ContactDataInfo>();
        }

        public async Task DisposeExpiredDataChangesAsync(int? maxCount, CancellationToken cancellationToken)
        {
            const int SecondsExpired = 60;
            var expiredThreshold = DateTime.Now.Subtract(TimeSpan.FromSeconds(SecondsExpired));

            KeyValuePair<string, (DateTime, DataChanged)>[] expiredCacheItems = null;
            lock (this.backplaneChangesLock)
            {
                // Note: next block will remove the 'stale' changes
                var possibleExpiredCacheItems = this.backplaneChanges.Where(kvp => kvp.Value.Item1 < expiredThreshold);
                if (maxCount.HasValue)
                {
                    possibleExpiredCacheItems = possibleExpiredCacheItems.Take(maxCount.Value);
                }

                if (possibleExpiredCacheItems.Any())
                {
                    expiredCacheItems = possibleExpiredCacheItems.ToArray();
                    foreach (var key in expiredCacheItems.Select(i => i.Key))
                    {
                        this.backplaneChanges.Remove(key);
                    }
                }
            }

            if (expiredCacheItems?.Length > 0)
            {
                // have the backplane providers to dispose this items
                await DisposeDataChangesAsync(expiredCacheItems.Select(i => i.Value.Item2).ToArray(), cancellationToken);
            }
        }

        public bool TrackDataChanged(DataChanged dataChanged)
        {
            lock (this.backplaneChangesLock)
            {
                if (this.backplaneChanges.ContainsKey(dataChanged.ChangeId))
                {
                    return true;
                }

                // track this data changed
                this.backplaneChanges.Add(dataChanged.ChangeId, (DateTime.Now, dataChanged));
                return false;
            }
        }

        private async Task UpdateBackplaneMetrics(CancellationToken cancellationToken)
        {
            if (MetricsFactory != null)
            {
                var metrics = MetricsFactory();
                // update metrics
                await UpdateBackplaneMetricsWithLogging(metrics.Item1, metrics.Item2, cancellationToken);
            }
        }

        private async Task OnContactChangedAsync(
            IContactBackplaneProvider backplaneProvider,
            ContactDataChanged<ContactDataInfo> contactDataChanged,
            string[] affectedProperties,
            CancellationToken cancellationToken)
        {
            if (TrackDataChanged(contactDataChanged))
            {
                return;
            }

            Logger.LogDebug($"OnContactChangedAsync from backplane provider:{backplaneProvider.GetType().Name} changed Id:{contactDataChanged.ChangeId}");

            if (ContactChangedAsync != null)
            {
                await ContactChangedAsync.Invoke(contactDataChanged, affectedProperties, cancellationToken);
            }
        }

        private async Task OnMessageReceivedAsync(
            IContactBackplaneProvider backplaneProvider,
            string sourceId,
            MessageData messageData,
            CancellationToken cancellationToken)
        {
            if (TrackDataChanged(messageData))
            {
                return;
            }

            Logger.LogDebug($"OnMessageReceivedAsync from backplane provider:{backplaneProvider.GetType().Name} changed Id:{messageData.ChangeId}");
 
            if (MessageReceivedAsync != null)
            {
                await MessageReceivedAsync.Invoke(sourceId, messageData, cancellationToken);
            }
        }

        private Task UpdateBackplaneMetricsWithLogging(
            (string ServiceId, string Stamp) serviceInfo,
            ContactServiceMetrics metrics,
            CancellationToken cancellationToken)
        {
            const long OneMb = 1024 * 1024;

            using (var proc = System.Diagnostics.Process.GetCurrentProcess())
            {
                using (Logger.BeginScope(
                    (LoggerScopeHelpers.MethodScope, MethodUpdateBackplaneMetrics),
                    (TotalContactsProperty, metrics.SelfCount),
                    (TotalConnectionsProperty, metrics.TotalSelfCount),
                    (MemorySizeProperty, proc.WorkingSet64 / OneMb),
                    (TotalMemoryProperty, GC.GetTotalMemory(false) / OneMb)))
                {
                    Logger.LogInformation($"serviceInfo:{serviceInfo}");
                }
            }

            return UpdateBackplaneMetrics(serviceInfo, metrics, cancellationToken);
        }

        private async Task DisposeDataChangesAsync(
            DataChanged[] dataChanges,
            CancellationToken cancellationToken)
        {
            using (Logger.BeginSingleScope(
                 (LoggerScopeHelpers.MethodScope, MethodDisposeDataChanges)))
            {
                Logger.LogDebug($"size:{dataChanges.Length}");
            }

            foreach (var backplaneProvider in GetBackPlaneProvidersByPriority())
            {
                try
                {
                    await backplaneProvider.DisposeDataChangesAsync(dataChanges, cancellationToken);
                }
                catch (Exception error)
                {
                    HandleBackplaneProviderException(backplaneProvider, nameof(IContactBackplaneProvider.DisposeDataChangesAsync), error);
                }
            }
        }

        private IEnumerable<IContactBackplaneProvider> GetBackPlaneProvidersByPriority()
        {
            return BackplaneProviders.OrderByDescending(p => p.Priority);
        }

        private void HandleBackplaneProviderException(IContactBackplaneProvider backplaneProvider, string methodName, Exception error)
        {
            if (!backplaneProvider.HandleException(methodName, error))
            {
                if (ShouldLogException(error))
                {
                    Logger.LogWarning(error, $"Failed to invoke method:{methodName} on provider:{backplaneProvider.GetType().Name}");
                }
            }
        }

        /// <summary>
        /// Return true when this type of exception should be logged as an error to report in our telemetry
        /// </summary>
        /// <param name="error">The error instance</param>
        /// <returns></returns>
        private static bool ShouldLogException(Exception error)
        {
            return ! (
                error is OperationCanceledException ||
                error.GetType().Name == "ServiceUnavailableException");
        }
    }
}
