// <copyright file="ContactBackplaneManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.Services.Backplane.Common;
using Microsoft.VsCloudKernel.SignalService.Common;
using ConnectionProperties = System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>;
using ContactDataInfo = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>>;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// A backplane manager that can host multiple backplane providers.
    /// </summary>
    public class ContactBackplaneManager : BackplaneManagerBase<IContactBackplaneProvider, ContactBackplaneProviderSupportLevel, ContactServiceMetrics>,  IContactBackplaneManager
    {
        private const string TotalContactsProperty = "NumberOfContacts";
        private const string TotalConnectionsProperty = "NumberOfConnections";

        public ContactBackplaneManager(ILogger<ContactBackplaneManager> logger, IDataFormatProvider formatProvider = null)
            : base(logger, formatProvider)
        {
        }

        public event OnContactChangedAsync ContactChangedAsync;

        public event OnMessageReceivedAsync MessageReceivedAsync;

        public async Task UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged, CancellationToken cancellationToken)
        {
            await WaitAll(
                GetSupportedProviders(s => s.UpdateContact).Select(p => (p.UpdateContactAsync(contactDataChanged, cancellationToken) as Task, p)),
                nameof(IContactBackplaneProvider.UpdateContactAsync),
                $"contactId:{ToTraceText(contactDataChanged.ContactId)}");
        }

        public async Task UpdateContactDataInfoAsync(
            ContactDataChanged<ConnectionProperties> contactDataChanged,
            (ContactDataInfo NewValue, ContactDataInfo OldValue) contactDataInfoValues,
            CancellationToken cancellationToken)
        {
            await WaitAll(
                GetSupportedProviders(s => s.UpdateContact).Select(p => (p.UpdateContactDataInfoAsync(contactDataChanged, contactDataInfoValues, cancellationToken) as Task, p)),
                nameof(IContactBackplaneProvider.UpdateContactDataInfoAsync),
                $"contactId:{ToTraceText(contactDataChanged.ContactId)}");
        }

        public async Task SendMessageAsync(
            MessageData messageData,
            CancellationToken cancellationToken)
        {
            await WaitAll(
                GetSupportedProviders(s => s.SendMessage).Select(p => (p.SendMessageAsync(messageData, cancellationToken), p)),
                nameof(IContactBackplaneProvider.SendMessageAsync),
                $"from:{ToTraceText(messageData.FromContact.Id)} to:{ToTraceText(messageData.TargetContact.Id)}");
        }

        public async Task<ContactDataInfo> GetContactDataAsync(string contactId, CancellationToken cancellationToken)
        {
            return await WaitFirstOrDefault(
                GetSupportedProviders(s => s.GetContact).Select(p => (p.GetContactDataAsync(contactId, cancellationToken), p)),
                nameof(IContactBackplaneProvider.GetContactDataAsync),
                (contactDataInfo) => $"contactId:{ToTraceText(contactId)} result count:{contactDataInfo?.Count}",
                r => r != null);
        }

        public async Task<Dictionary<string, ContactDataInfo>[]> GetContactsDataAsync(Dictionary<string, object>[] matchProperties, CancellationToken cancellationToken)
        {
            var result = await WaitFirstOrDefault(
                GetSupportedProviders(s => s.GetContacts).Select(p => (p.GetContactsDataAsync(matchProperties, cancellationToken), p)),
                nameof(IContactBackplaneProvider.GetContactsDataAsync),
                (matches) =>
                {
                    var sb = new StringBuilder();
                    for (var index = 0; index < matchProperties.Length; ++index)
                    {
                        if (sb.Length > 0)
                        {
                            sb.AppendLine();
                        }

                        sb.Append($"matchProperties:{matchProperties[index].ConvertToString(FormatProvider)} result count:{matches?[index]?.Count ?? 0}");
                    }

                    return sb.ToString();
                },
                results => results?.Count(i => i != null) == matchProperties.Length);

            return result ?? new Dictionary<string, ContactDataInfo>[matchProperties.Length];
        }

        protected override void OnRegisterProvider(IContactBackplaneProvider backplaneProvider)
        {
            backplaneProvider.ContactChangedAsync = (contactDataChanged, affectedProperties, ct) => OnContactChangedAsync(backplaneProvider, contactDataChanged, affectedProperties, ct);
            backplaneProvider.MessageReceivedAsync = (messageData, ct) => OnMessageReceivedAsync(backplaneProvider, messageData, ct);
        }

        protected override void AddMetricsScope(List<(string, object)> metricsScope, ContactServiceMetrics metrics)
        {
            metricsScope.Add((TotalContactsProperty, metrics.SelfCount));
            metricsScope.Add((TotalConnectionsProperty, metrics.TotalSelfCount));
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

            var stopWatch = Stopwatch.StartNew();
            if (ContactChangedAsync != null)
            {
                await ContactChangedAsync.Invoke(contactDataChanged, affectedProperties, cancellationToken);
            }

            Logger.LogScope(
                LogLevel.Debug,
                $"provider:{backplaneProvider.GetType().Name} changed Id:{contactDataChanged.ChangeId}",
                (LoggerScopeHelpers.MethodScope, nameof(OnContactChangedAsync)),
                (LoggerScopeHelpers.MethodPerfScope, stopWatch.ElapsedMilliseconds));
        }

        private async Task OnMessageReceivedAsync(
            IContactBackplaneProvider backplaneProvider,
            MessageData messageData,
            CancellationToken cancellationToken)
        {
            if (HasTrackDataChanged(messageData))
            {
                return;
            }

            var stopWatch = Stopwatch.StartNew();
            if (MessageReceivedAsync != null)
            {
                await MessageReceivedAsync.Invoke(messageData, cancellationToken);
            }

            Logger.LogScope(
                LogLevel.Debug,
                $"provider:{backplaneProvider.GetType().Name} changed Id:{messageData.ChangeId}",
                (LoggerScopeHelpers.MethodScope, nameof(OnMessageReceivedAsync)),
                (LoggerScopeHelpers.MethodPerfScope, stopWatch.ElapsedMilliseconds));
        }
    }
}
