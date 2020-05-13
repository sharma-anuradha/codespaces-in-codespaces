// <copyright file="ContactBackplaneService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;
using ContactDataInfo = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>>;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// Our backplane service that will handle multiple contact service request.
    /// </summary>
    public class ContactBackplaneService : BackplaneService<IContactBackplaneManager, IContactBackplaneServiceNotification>, IHostedService
    {
        private const string TotalContactsProperty = "NumberOfContacts";
        private const string TotalConnectionsProperty = "NumberOfConnections";
        private const string TotalSignalRConnectionsProperty = "NumberOfSignalRConnections";

        private const string TotalUpdateContactsProperty = "NumberOfUpdateContacts";

        /// <summary>
        /// Max number of contact items to buffer before we start blocking.
        /// </summary>
        private const int MaxContactsBuffer = 15000;

        /// <summary>
        /// Max number of messages to buffer before we start blocking.
        /// </summary>
        private const int MaxMessageBuffer = 1000;

        /// <summary>
        /// max time to allow an update contact job to wait on the queue.
        /// </summary>
        private static readonly TimeSpan MaxUpdateContactQueueWaiTime = TimeSpan.FromSeconds(30);

        private readonly ConcurrentDictionary<string, (DateTime, ContactServiceMetrics)> activeServices = new ConcurrentDictionary<string, (DateTime, ContactServiceMetrics)>();
        private readonly IStartupBase startupBase;
        private readonly IDataFormatProvider formatProvider;

        /// <summary>
        /// The update queue count.
        /// </summary>
        private int updateQueueCount;

        /// <summary>
        /// The number of throttled contacts.
        /// </summary>
        private int throttledContactsCount;

        /// <summary>
        /// Count of the processed update contacts on our TPL action block.
        /// </summary>
        private int updateContactActionBlockPerfCounter;

        /// <summary>
        /// the accumulated waiting time on an update job in ms.
        /// </summary>
        private long updateContactWaitingTimePerfCounter;

        public ContactBackplaneService(
            IEnumerable<IContactBackplaneServiceNotification> contactBackplaneServiceNotifications,
            ILogger<ContactBackplaneService> logger,
            IBackplaneServiceDataProvider serviceDataProvider,
            IContactBackplaneManager backplaneManager,
            IStartupBase startupBase,
            IOptions<AppSettings> appSettingsProvider,
            IServiceCounters serviceCounters = null,
            IDataFormatProvider formatProvider = null)
            : base(backplaneManager, contactBackplaneServiceNotifications, logger)
        {
            ServiceDataProvider = Requires.NotNull(serviceDataProvider, nameof(serviceDataProvider));
            this.startupBase = Requires.NotNull(startupBase, nameof(startupBase));
            this.formatProvider = formatProvider;
            ServiceCounters = serviceCounters;

            BackplaneManager.ContactChangedAsync += OnContactChangedAsync;
            BackplaneManager.MessageReceivedAsync += OnMessageReceivedAsync;

            const int MaxDegreeOfParallelism = 32;

            UpdateContactActionBlock = CreateActionBlock<(ContactDataChanged<ConnectionProperties>, (ContactDataInfo NewValue, ContactDataInfo OldValue))>(
                nameof(UpdateContactActionBlock),
                async (elapsed, updateContactInfo) =>
                {
                    // update queue count
                    Interlocked.Decrement(ref this.updateQueueCount);

                    // update accumulated waiting time perf counter
                    Interlocked.Add(ref this.updateContactWaitingTimePerfCounter, elapsed.Milliseconds);

                    // next block will start/stop throttling
                    if (!IsGlobalUpdateContactThrottle)
                    {
                        if (elapsed > MaxUpdateContactQueueWaiTime && this.updateQueueCount > MaxContactsBuffer * 1.2)
                        {
                            IsGlobalUpdateContactThrottle = true;
                            Logger.LogMethodScope(LogLevel.Warning, "Throttled started", nameof(IsGlobalUpdateContactThrottle));
                        }
                    }
                    else
                    {
                        if (this.updateQueueCount < MaxContactsBuffer)
                        {
                            IsGlobalUpdateContactThrottle = false;
                            Logger.LogMethodScope(LogLevel.Warning, $"Throttled stopped. count:{this.throttledContactsCount}", nameof(IsGlobalUpdateContactThrottle));
                            this.throttledContactsCount = 0;
                        }
                    }

                    try
                    {
                        await BackplaneManager.UpdateContactDataInfoAsync(updateContactInfo.Item1, updateContactInfo.Item2, DisposeToken);
                        Interlocked.Increment(ref this.updateContactActionBlockPerfCounter);
                    }
                    finally
                    {
                        TrackDataChanged(updateContactInfo.Item1, TrackDataChangedOptions.Refresh);
                    }
                },
                item => item.Item1.ChangeId,
                MaxDegreeOfParallelism,
                MaxContactsBuffer);

            SendMessageActionBlock = CreateActionBlock<MessageData>(
                nameof(SendMessageActionBlock),
                async (elapsed, sendMessageInfo) =>
                {
                    try
                    {
                        await BackplaneManager.SendMessageAsync(sendMessageInfo, DisposeToken);
                    }
                    finally
                    {
                        TrackDataChanged(sendMessageInfo, TrackDataChangedOptions.Refresh);
                    }
                },
                item => item.ChangeId,
                MaxDegreeOfParallelism,
                MaxMessageBuffer);

            int oldWorkerThreads, oldCompletionPortThreads;
            ThreadPool.GetMinThreads(out oldWorkerThreads, out oldCompletionPortThreads);

            // define min threads pool
            int workerThreads = appSettingsProvider.Value.WorkerThreads;
            int completionPortThreads = appSettingsProvider.Value.CompletionPortThreads;

            bool succeeded = ThreadPool.SetMinThreads(workerThreads, completionPortThreads);

            Logger.LogInformation($"BackplaneService created succeeded:{succeeded} workerThreads:{workerThreads}:{oldWorkerThreads} completionPortThreads:{completionPortThreads}:{oldCompletionPortThreads}");
        }

        private ActionBlock<(Stopwatch, (ContactDataChanged<ConnectionProperties>, (ContactDataInfo NewValue, ContactDataInfo OldValue)))> UpdateContactActionBlock { get; }

        private ActionBlock<(Stopwatch, MessageData)> SendMessageActionBlock { get; }

        private IBackplaneServiceDataProvider ServiceDataProvider { get; }

        private IServiceCounters ServiceCounters { get; }

        /// <summary>
        /// Gets or sets a value indicating whether update global contacts are being throttled.
        /// </summary>
        private bool IsGlobalUpdateContactThrottle { get; set; }

        public async Task RunAsync(CancellationToken stoppingToken)
        {
            const int TimespanUpdateSecs = 15;
            const int TimespanUpdateTelemetrySecs = 60;

            var updateMetricsCounter = new SecondsCounter(BackplaneManagerConst.UpdateMetricsSecs, TimespanUpdateSecs);
            var updateTelemetryCounter = new SecondsCounter(TimespanUpdateTelemetrySecs, TimespanUpdateSecs);

            ResetPerfCounters();
            while (true)
            {
                // wait
                await Task.Delay(TimeSpan.FromSeconds(TimespanUpdateSecs), stoppingToken);

                // update aggregated metrics
                if (updateMetricsCounter.Next())
                {
                    await BackplaneManager.UpdateBackplaneMetricsAsync(
                        this.startupBase.ServiceInfo,
                        GetAggregatedMetrics(),
                        stoppingToken);
                }

                // update telemetry metrics
                if (updateTelemetryCounter.Next())
                {
                    var aggregatedMetrics = GetAggregatedMetrics();

                    using (Logger.BeginScope(
                        (LoggerScopeHelpers.MethodScope, "UpdateMetrics"),
                        (TotalUpdateContactsProperty, UpdateContactActionBlock.InputCount),
                        (BackplaneManagerConst.BackplaneChangesCountProperty, BackplaneManager.BackplaneChangesCount),
                        (TotalContactsProperty, ServiceDataProvider.TotalContacts),
                        (TotalConnectionsProperty, ServiceDataProvider.TotalConnections),
                        (TotalSignalRConnectionsProperty, aggregatedMetrics.TotalSelfCount)))
                    {
                        var perfCountersPerMinute = new List<int>();
                        perfCountersPerMinute.Add(this.updateContactActionBlockPerfCounter / TimespanUpdateSecs);
                        var totalProcessedUpdates = this.updateContactActionBlockPerfCounter;
                        var averageWaitingTime = totalProcessedUpdates != 0 ? this.updateContactWaitingTimePerfCounter / totalProcessedUpdates : 0;
                        Logger.LogInformation($"Queue :{this.updateQueueCount}, Throttle:{IsGlobalUpdateContactThrottle}, Wait time(ms):{averageWaitingTime}, Perf (events x secs):[{string.Join(',', perfCountersPerMinute)}]");
                    }

                    ResetPerfCounters();
                }
            }
        }

        public async Task DisposeAsync()
        {
            await CompleteActionBlockAsync(UpdateContactActionBlock, nameof(UpdateContactActionBlock));
            await CompleteActionBlockAsync(SendMessageActionBlock, nameof(SendMessageActionBlock));
        }

        public Task UpdateMetricsAsync(
            ServiceInfo serviceInfo,
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

            return BackplaneManager.UpdateBackplaneMetricsAsync(serviceInfo, metrics, cancellationToken);
        }

        public async Task UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();

            using (Logger.BeginMethodScope(nameof(UpdateContactAsync)))
            {
                Logger.LogDebug($"contactId:{ToString(contactDataChanged)}");
            }

            TrackDataChanged(contactDataChanged, TrackDataChangedOptions.Lock);

            (ContactDataInfo NewValue, ContactDataInfo OldValue) contactDataInfoValues;
            if (await ServiceDataProvider.ContainsContactAsync(contactDataChanged.ContactId, cancellationToken))
            {
                // we already know about this contact, simple update
                contactDataInfoValues = await ServiceDataProvider.UpdateContactDataChangedAsync(contactDataChanged, cancellationToken);
            }
            else
            {
                // first get the global data for this contact
                var contactDataInfo = (await BackplaneManager.GetContactDataAsync(contactDataChanged.ContactId, cancellationToken)) ??
                    new Dictionary<string, IDictionary<string, ConnectionProperties>>();

                // merge the data
                contactDataInfo.UpdateConnectionProperties(contactDataChanged);
                await ServiceDataProvider.UpdateContactDataInfoAsync(contactDataChanged.ContactId, contactDataInfo, cancellationToken);

                contactDataInfoValues = (contactDataInfo, null);
            }

            await FireOnUpdateContactAsync(contactDataChanged.Clone(contactDataInfoValues.NewValue), contactDataChanged.Data.Keys.ToArray(), cancellationToken);

            if (!IsGlobalUpdateContactThrottle)
            {
                // increment update queue count
                Interlocked.Increment(ref this.updateQueueCount);
                await UpdateContactActionBlock.SendAsync((Stopwatch.StartNew(), (contactDataChanged, contactDataInfoValues)), cancellationToken);
            }
            else
            {
                Interlocked.Increment(ref this.throttledContactsCount);
            }

            MethodPerf(nameof(UpdateContactAsync), sw.Elapsed);
        }

        public async Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            using (Logger.BeginMethodScope(nameof(GetContactDataAsync)))
            {
                Logger.LogDebug($"contactId:{Format("{0:T}", contactId)}");
            }

            var contactDataInfo = await ServiceDataProvider.GetContactDataAsync(contactId, cancellationToken);
            if (contactDataInfo == null)
            {
                contactDataInfo = await BackplaneManager.GetContactDataAsync(contactId, cancellationToken);
                if (contactDataInfo != null)
                {
                    await ServiceDataProvider.UpdateContactDataInfoAsync(contactId, contactDataInfo, cancellationToken);
                }
            }

            MethodPerf(nameof(GetContactDataAsync), sw.Elapsed);
            return contactDataInfo;
        }

        public async Task<Dictionary<string, ContactDataInfo>[]> GetContactsDataAsync(Dictionary<string, object>[] matchProperties, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            using (Logger.BeginMethodScope(nameof(GetContactsDataAsync)))
            {
                Logger.LogDebug($"emails:{string.Join(",", matchProperties.Select(i => Format("{0:E}", i.TryGetProperty<string>(ContactProperties.Email))))}");
            }

            var results = await ServiceDataProvider.GetContactsDataAsync(matchProperties, cancellationToken);
            var nonMatchResults = new List<int>();

            for (int index = 0; index < matchProperties.Length; ++index)
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

            MethodPerf(nameof(GetContactsDataAsync), sw.Elapsed);
            return results;
        }

        public async Task SendMessageAsync(MessageData messageData, CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(nameof(SendMessageAsync)))
            {
                Logger.LogDebug($"type:{messageData.Type}");
            }

            TrackDataChanged(messageData, TrackDataChangedOptions.Lock);
            await FireOnSendMessageAsync(messageData, cancellationToken);

            // route to global providers
            await SendMessageActionBlock.SendAsync((Stopwatch.StartNew(), messageData), cancellationToken);
        }

        private string ToString(ContactDataChanged<ConnectionProperties> contactDataChanged)
        {
            return Format("{0:T}", contactDataChanged.ContactId);
        }

        private bool IsLocalService(string serviceId) => this.activeServices.ContainsKey(serviceId);

        private void ResetPerfCounters()
        {
            this.updateContactActionBlockPerfCounter = 0;
            this.updateContactWaitingTimePerfCounter = 0;
        }

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
            var sw = Stopwatch.StartNew();
            using (Logger.BeginMethodScope(nameof(OnContactChangedAsync)))
            {
                Logger.LogDebug($"contactId:{Format("{0:T}", contactDataChanged.ContactId)} local:{IsLocalService(contactDataChanged.ServiceId)}");
            }

            // Note: we only update if the contacts was tracked in the cluster
            if (await ServiceDataProvider.ContainsContactAsync(contactDataChanged.ContactId, cancellationToken))
            {
                await ServiceDataProvider.UpdateContactDataInfoAsync(contactDataChanged.ContactId, contactDataChanged.Data, cancellationToken);
            }

            await FireOnUpdateContactAsync(contactDataChanged, affectedProperties, cancellationToken);
            MethodPerf(nameof(OnContactChangedAsync), sw.Elapsed);
        }

        private async Task OnMessageReceivedAsync(
            MessageData messageData,
            CancellationToken cancellationToken)
        {
            using (Logger.BeginMethodScope(nameof(OnMessageReceivedAsync)))
            {
                Logger.LogDebug($"type:{messageData.Type} local:{IsLocalService(messageData.ServiceId)}");
            }

            await FireOnSendMessageAsync(messageData, cancellationToken);
        }

        private async Task FireOnUpdateContactAsync(ContactDataChanged<ContactDataInfo> contactDataChanged, string[] affectedProperties, CancellationToken cancellationToken)
        {
            Logger.LogMethodScope(
                LogLevel.Debug,
                $"contactId:{Format("{0:T}", contactDataChanged.ContactId)} affectedProperties:{(affectedProperties != null ? string.Join(",", affectedProperties) : "<null>")}",
                nameof(FireOnUpdateContactAsync));
            foreach (var notify in BackplaneServiceNotifications)
            {
                await notify.FireOnUpdateContactAsync(contactDataChanged, affectedProperties, cancellationToken);
            }
        }

        private async Task FireOnSendMessageAsync(MessageData messageData, CancellationToken cancellationToken)
        {
            Logger.LogMethodScope(
               LogLevel.Debug,
               $"serviceId:{messageData.ServiceId} from:{Format("{0:T}", messageData.FromContact.Id)} to:{Format("{0:T}", messageData.TargetContact.Id)}",
               nameof(FireOnSendMessageAsync));
            foreach (var notify in BackplaneServiceNotifications)
            {
                await notify.FireOnSendMessageAsync(messageData, cancellationToken);
            }
        }

        private void MethodPerf(string methodName, TimeSpan t)
        {
            ServiceCounters?.OnInvokeMethod(nameof(ContactBackplaneService), methodName, t);
        }
    }
}
