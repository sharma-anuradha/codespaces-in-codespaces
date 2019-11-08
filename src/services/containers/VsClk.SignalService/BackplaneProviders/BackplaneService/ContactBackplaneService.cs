using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService.Common;
using Microsoft.Extensions.Options;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// Our backplane service that will handle multiple contact service request
    /// </summary>
    public class ContactBackplaneService
    {
        private readonly ConcurrentDictionary<string, (DateTime, ContactServiceMetrics)> activeServices = new ConcurrentDictionary<string, (DateTime, ContactServiceMetrics)>();
        private readonly IStartupBase startupBase;
        private readonly IDataFormatProvider formatProvider;
        private int numOfConnections;
        private List<IContactBackplaneServiceNotification> contactBackplaneServiceNotifications = new List<IContactBackplaneServiceNotification>();
        
        public ContactBackplaneService(
            IEnumerable<IContactBackplaneServiceNotification> contactBackplaneServiceNotifications,
            ILogger<ContactBackplaneService> logger,
            IBackplaneServiceDataProvider serviceDataProvider,
            IContactBackplaneManager backplaneManager,
            IStartupBase startupBase,
            IOptions<AppSettings> appSettingsProvider,
            IDataFormatProvider formatProvider = null)
        {
            Logger = Requires.NotNull(logger, nameof(logger));
            this.contactBackplaneServiceNotifications.AddRange(contactBackplaneServiceNotifications);

            ServiceDataProvider = Requires.NotNull(serviceDataProvider, nameof(serviceDataProvider));
            BackplaneManager = Requires.NotNull(backplaneManager, nameof(backplaneManager));
            this.startupBase = Requires.NotNull(startupBase, nameof(startupBase));
            this.formatProvider = formatProvider;

            BackplaneManager.ContactChangedAsync += OnContactChangedAsync;
            BackplaneManager.MessageReceivedAsync += OnMessageReceivedAsync;

            int oldWorkerThreads, oldCompletionPortThreads;
            ThreadPool.GetMinThreads(out oldWorkerThreads, out oldCompletionPortThreads);

            // define min threads pool
            int workerThreads = appSettingsProvider.Value.WorkerThreads;
            int completionPortThreads = appSettingsProvider.Value.CompletionPortThreads;

            bool succeeded = ThreadPool.SetMinThreads(workerThreads, completionPortThreads);

            Logger.LogInformation($"BackplaneService created succeeded:{succeeded} workerThreads:{workerThreads}:{oldWorkerThreads} completionPortThreads:{completionPortThreads}:{oldCompletionPortThreads}");
        }

        private ILogger Logger { get; }

        private IContactBackplaneManager BackplaneManager { get; }

        private IBackplaneServiceDataProvider ServiceDataProvider { get; }

        public void AddContactBackplaneServiceNotification(IContactBackplaneServiceNotification contactBackplaneServiceNotification)
        {
            this.contactBackplaneServiceNotifications.Add(contactBackplaneServiceNotification);
        }

        #region Hub methods

        public void RegisterService(string serviceId)
        {
            Interlocked.Increment(ref this.numOfConnections);
            Logger.LogInformation($"RegisterService -> serviceId:{serviceId} numOfConnections:{this.numOfConnections}");
        }

        public void OnDisconnected(string serviceId, Exception exception)
        {
            Interlocked.Decrement(ref this.numOfConnections);
            Logger.LogInformation(exception, $"OnDisconnectedAsync -> serviceId:{serviceId} numOfConnections:{this.numOfConnections} error:{exception?.Message}");

        }

        public Task UpdateMetricsAsync(
            (string ServiceId, string Stamp) serviceInfo,
            ContactServiceMetrics metrics,
            CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(nameof(UpdateMetricsAsync)))
            {
                Logger.LogDebug($"serviceId:{serviceInfo.ServiceId}");
            }

            var now = DateTime.Now;
            var expiredThreshold = now.Subtract(TimeSpan.FromSeconds(BackplaneManagerConst.StaleServiceSeconds));

            var changed = !this.activeServices.ContainsKey(serviceInfo.ServiceId);

            this.activeServices[serviceInfo.ServiceId] = (now, metrics);
            var expiredServices = this.activeServices.ToArray().Where(kvp => kvp.Value.Item1 < expiredThreshold);
            changed = changed || expiredServices.Any();
            if (expiredServices.Any())
            {
                foreach (var kvp in expiredServices)
                {
                    this.activeServices.TryRemove(kvp.Key, out var lastSeen);
                }
            }

            if (changed)
            {
                ServiceDataProvider.ActiveServices = this.activeServices.Keys.ToArray();
            }

            return BackplaneManager.UpdateBackplaneMetrics(serviceInfo, metrics, cancellationToken);
        }

        public async Task UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(nameof(UpdateContactAsync)))
            {
                Logger.LogDebug($"contactId:{Format("{0:T}",contactDataChanged.ContactId)}");
            }

            BackplaneManager.TrackDataChanged(contactDataChanged);

            ContactDataInfo contactDataInfo;
            if (await ServiceDataProvider.ContainsContactAsync(contactDataChanged.ContactId, cancellationToken))
            {
                // we already know about this contact, simple update
                contactDataInfo = await ServiceDataProvider.UpdateContactAsync(contactDataChanged, cancellationToken);
            }
            else
            {
                // first get the global data for this contact 
                contactDataInfo = (await BackplaneManager.GetContactDataAsync(contactDataChanged.ContactId, cancellationToken)) ??
                    new Dictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>();
                
                // merge the data
                contactDataInfo.UpdateConnectionProperties(contactDataChanged);
                await ServiceDataProvider.UpdateContactDataInfoAsync(contactDataChanged.ContactId, contactDataInfo, cancellationToken);
            }

            await FireOnUpdateContactAsync(contactDataChanged.Clone(contactDataInfo), contactDataChanged.Data.Keys.ToArray(), cancellationToken);

            // update the global providers
            BackplaneManager.UpdateContactAsync(contactDataChanged, cancellationToken).Forget();
        }

        public async Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(nameof(GetContactDataAsync)))
            {
                Logger.LogDebug($"contactId:{Format("{0:T}", contactId)}");
            }

            var contactDataInfo = await ServiceDataProvider.GetContactDataAsync(contactId, cancellationToken);
            if (contactDataInfo == null)
            {
                contactDataInfo = await BackplaneManager.GetContactDataAsync(contactId, cancellationToken);
            }

            return contactDataInfo;
        }

        public async Task<Dictionary<string, ContactDataInfo>> GetContactsDataAsync(Dictionary<string, object> matchProperties, CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(nameof(GetContactsDataAsync)))
            {
                Logger.LogDebug($"email:{Format("{0:E}",matchProperties.TryGetProperty<string>(ContactProperties.Email))}");
            }

            var result = await ServiceDataProvider.GetContactsDataAsync(matchProperties, cancellationToken);
            if (result.Count == 0)
            {
                result = await BackplaneManager.GetContactsDataAsync(matchProperties, cancellationToken);
            }

            return result;
        }

        public async Task SendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(nameof(SendMessageAsync)))
            {
                Logger.LogDebug($"type:{messageData.Type}");
            }

            BackplaneManager.TrackDataChanged(messageData);
            await FireOnSendMessageAsync(sourceId, messageData, cancellationToken);

            BackplaneManager.SendMessageAsync(sourceId, messageData, cancellationToken).Forget();
        }

        #endregion

        private bool IsLocalService(string serviceId) => this.activeServices.ContainsKey(serviceId);

        private ContactServiceMetrics GetAggregatedMetrics()
        {
            int count = 0;
            int selfCount = 0;
            int totalSelfCount = 0;
            int stubCount = 0;

            foreach (var srvc in this.activeServices.Values)
            {
                count += srvc.Item2.Count;
                selfCount += srvc.Item2.SelfCount;
                totalSelfCount += srvc.Item2.TotalSelfCount;
                stubCount += srvc.Item2.StubCount;
            }

            var serviceMetrics = new ContactServiceMetrics(count, selfCount, totalSelfCount, stubCount);
            return serviceMetrics;
        }

        private string Format(string format, params object[] args)
        {
            return string.Format(this.formatProvider, format, args);
        }

        private async Task OnContactChangedAsync(
            ContactDataChanged<ContactDataInfo> contactDataChanged,
            string[] affectedProperties,
            CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(nameof(OnContactChangedAsync)))
            {
                Logger.LogDebug($"contactId:{Format("{0:T}", contactDataChanged.ContactId)} local:{IsLocalService(contactDataChanged.ServiceId)}");
            }

            if (await ServiceDataProvider.ContainsContactAsync(contactDataChanged.ContactId, cancellationToken))
            {
                await ServiceDataProvider.UpdateContactDataInfoAsync(contactDataChanged.ContactId, contactDataChanged.Data, cancellationToken);
            }

            await FireOnUpdateContactAsync(contactDataChanged, affectedProperties, cancellationToken);
        }

        private async Task OnMessageReceivedAsync(
            string sourceId,
            MessageData messageData,
            CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(nameof(OnMessageReceivedAsync)))
            {
                Logger.LogDebug($"type:{messageData.Type} local:{IsLocalService(sourceId)}");
            }

            await FireOnSendMessageAsync(sourceId, messageData, cancellationToken);
        }

        private async Task FireOnUpdateContactAsync(ContactDataChanged<ContactDataInfo> contactDataChanged, string[] affectedProperties, CancellationToken cancellationToken)
        {
            Logger.LogMethodScope(LogLevel.Debug,
                $"contactId:{Format("{0:T}", contactDataChanged.ContactId)} affectedProperties:{(affectedProperties != null ? string.Join(",", affectedProperties) : "<null>")}",
                nameof(FireOnUpdateContactAsync));
            foreach (var notify in this.contactBackplaneServiceNotifications)
            {
                await notify.FireOnUpdateContactAsync(contactDataChanged, affectedProperties, cancellationToken);
            }
        }

        private async Task FireOnSendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
        {
            Logger.LogMethodScope(
               LogLevel.Debug,
               $"serviceId:{sourceId} from:{Format("{0:T}", messageData.FromContact.Id)} to:{Format("{0:T}", messageData.TargetContact.Id)}",
               nameof(FireOnSendMessageAsync));
            foreach (var notify in this.contactBackplaneServiceNotifications)
            {
                await notify.FireOnSendMessageAsync(sourceId, messageData, cancellationToken);
            }
        }
    }
}
