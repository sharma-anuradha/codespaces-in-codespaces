using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Common;

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

        private readonly ActionBlock<(Stopwatch, ContactDataChanged<ConnectionProperties>)> updateContactActionBlock;
        private readonly ActionBlock<(Stopwatch, string, MessageData)> sendMessageActionBlock;
        private readonly CancellationTokenSource disposeTokenSource = new CancellationTokenSource();

        private int updateContactPerfCounter;

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

            var blockOptions = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = 32
            };

            this.updateContactActionBlock = new ActionBlock<(Stopwatch, ContactDataChanged<ConnectionProperties>)>(async (updateContactInfo) =>
            {
                try
                {
                    await BackplaneManager.UpdateContactAsync(updateContactInfo.Item2, DisposeToken);
                    Logger.LogMethodScope(LogLevel.Debug, $"input count:{this.updateContactActionBlock.InputCount}", nameof(this.updateContactActionBlock), updateContactInfo.Item1.ElapsedMilliseconds);
                    Interlocked.Increment(ref updateContactPerfCounter);
                }
                catch(Exception error)
                {
                    Logger.LogWarning(error,$"Failed to update contact change id:{updateContactInfo.Item2.ChangeId} contact Id:{updateContactInfo.Item2}");
                }
            }, blockOptions);

            this.sendMessageActionBlock = new ActionBlock<(Stopwatch, string, MessageData)>(async (sendMessageInfo) =>
            {
                try
                {
                    await BackplaneManager.SendMessageAsync(sendMessageInfo.Item2, sendMessageInfo.Item3, DisposeToken);
                    Logger.LogMethodScope(LogLevel.Debug, $"input count:{this.sendMessageActionBlock.InputCount}", nameof(this.updateContactActionBlock), sendMessageInfo.Item1.ElapsedMilliseconds);
                }
                catch (Exception error)
                {
                    Logger.LogWarning(error, $"Failed to send message change id:{sendMessageInfo.Item3.ChangeId}");
                }
            }, blockOptions);

            int oldWorkerThreads, oldCompletionPortThreads;
            ThreadPool.GetMinThreads(out oldWorkerThreads, out oldCompletionPortThreads);

            // define min threads pool
            int workerThreads = appSettingsProvider.Value.WorkerThreads;
            int completionPortThreads = appSettingsProvider.Value.CompletionPortThreads;

            bool succeeded = ThreadPool.SetMinThreads(workerThreads, completionPortThreads);

            Logger.LogInformation($"BackplaneService created succeeded:{succeeded} workerThreads:{workerThreads}:{oldWorkerThreads} completionPortThreads:{completionPortThreads}:{oldCompletionPortThreads}");
        }

        private CancellationToken DisposeToken => this.disposeTokenSource.Token;

        private ILogger Logger { get; }

        private IContactBackplaneManager BackplaneManager { get; }

        private IBackplaneServiceDataProvider ServiceDataProvider { get; }

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            const int TimespanUpdateMin = 1;

            this.updateContactPerfCounter = 0;
            while (true)
            {
                // wait
                await Task.Delay(TimeSpan.FromMinutes(TimespanUpdateMin), stoppingToken);

                var memoryInfo = LoggerScopeHelpers.GetProcessMemoryInfo();
                using (Logger.BeginScope(
                    (LoggerScopeHelpers.MethodScope, "UpdateMetrics"),
                    (LoggerScopeHelpers.MemorySizeProperty, memoryInfo.memorySize),
                    (LoggerScopeHelpers.TotalMemoryProperty, memoryInfo.totalMemory)))
                {
                    Logger.LogInformation($"Contact perf:{this.updateContactPerfCounter/ TimespanUpdateMin}");
                }

                this.updateContactPerfCounter = 0;
            }
        }

        public async Task DisposeAsync()
        {
            await CompleteActionBlock(this.updateContactActionBlock, nameof(this.updateContactActionBlock));
            await CompleteActionBlock(this.sendMessageActionBlock, nameof(this.sendMessageActionBlock));
        }

        public void AddContactBackplaneServiceNotification(IContactBackplaneServiceNotification contactBackplaneServiceNotification)
        {
            this.contactBackplaneServiceNotifications.Add(contactBackplaneServiceNotification);
        }

        #region Hub methods

        public void RegisterService(string serviceId)
        {
            Interlocked.Increment(ref this.numOfConnections);
            Logger.LogMethodScope(LogLevel.Information, $"serviceId:{serviceId} numOfConnections:{this.numOfConnections}", nameof(RegisterService));
        }

        public void OnDisconnected(string serviceId, Exception exception)
        {
            Interlocked.Decrement(ref this.numOfConnections);
            Logger.LogMethodScope(LogLevel.Error, exception, $"OnDisconnectedAsync -> serviceId:{serviceId} numOfConnections:{this.numOfConnections}", nameof(OnDisconnected));
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
                Logger.LogDebug($"contactId:{ToString(contactDataChanged)}");
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
            await this.updateContactActionBlock.SendAsync((Stopwatch.StartNew(), contactDataChanged), cancellationToken);
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

        public async Task<Dictionary<string, ContactDataInfo>[]> GetContactsDataAsync(Dictionary<string, object>[] matchProperties, CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(nameof(GetContactsDataAsync)))
            {
                Logger.LogDebug($"emails:{string.Join(",", matchProperties.Select(i => Format("{0:E}",i.TryGetProperty<string>(ContactProperties.Email))))}");
            }

            var results = await ServiceDataProvider.GetContactsDataAsync(matchProperties, cancellationToken);
            var nonMatchResults = new List<int>();

            for(int index = 0; index < matchProperties.Length;++index)
            {
                if (results[index] == null)
                {
                    nonMatchResults.Add(index);
                }
            }

            if (nonMatchResults.Count > 0)
            {
                var backplaneResults = await BackplaneManager.GetContactsDataAsync(nonMatchResults.Select(i => matchProperties[i]).ToArray(), cancellationToken);
                Assumes.Equals(backplaneResults.Length, nonMatchResults.Count);

                for (int index = 0; index < backplaneResults.Length; ++index)
                {
                    results[nonMatchResults[index]] = backplaneResults[index];
                }
            }

            return results;
        }

        public async Task SendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(nameof(SendMessageAsync)))
            {
                Logger.LogDebug($"type:{messageData.Type}");
            }

            BackplaneManager.TrackDataChanged(messageData);
            await FireOnSendMessageAsync(sourceId, messageData, cancellationToken);

            // route to global providers
            await this.sendMessageActionBlock.SendAsync((Stopwatch.StartNew(), sourceId, messageData), cancellationToken);
        }

        #endregion

        private string ToString(ContactDataChanged<ConnectionProperties> contactDataChanged)
        {
            return Format("{0:T}", contactDataChanged.ContactId);
        }

        private Task CompleteActionBlock<T>(ActionBlock<T> actionBlock, string name)
        {
            Logger.LogDebug($"CompleteActionBlock for:{name} input count:{actionBlock.InputCount}");
            actionBlock.Complete();
            return actionBlock.Completion;
        }

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
