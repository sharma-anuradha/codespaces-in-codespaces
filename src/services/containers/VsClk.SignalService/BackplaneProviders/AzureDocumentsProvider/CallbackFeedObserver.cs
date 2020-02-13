// <copyright file="CallbackFeedObserver.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor.FeedProcessing;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.SignalService
{
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

    /// <summary>
    /// Implements Azure documents feed observer factory
    /// </summary>
    internal class CallbackFeedObserverFactory : IChangeFeedObserverFactory
    {
        private readonly string feedName;
        private readonly Func<IReadOnlyList<Document>, Task> onDcoumentsChanged;
        private readonly ILogger logger;

        public CallbackFeedObserverFactory(string feedName, Func<IReadOnlyList<Document>, Task> onDocumentsChanged, ILogger logger)
        {
            this.feedName = feedName;
            this.onDcoumentsChanged = onDocumentsChanged;
            this.logger = logger;
        }

        public IChangeFeedObserver CreateObserver()
        {
            return new CallbackFeedObserver(this.feedName, this.onDcoumentsChanged, this.logger);
        }
    }

    /// <summary>
    /// Implements interface IChangeFeedObserver
    /// </summary>
    internal class CallbackFeedObserver : IChangeFeedObserver
    {
        // Logger method scopes
        private const string MethodClose = "CallbackFeedObserver.Close";
        private const string MethodOpen = "CallbackFeedObserver.Open";

        private readonly string feedName;
        private readonly Func<IReadOnlyList<Document>, Task> onDocumentsChanged;
        private readonly ILogger logger;

        public CallbackFeedObserver(string feedName, Func<IReadOnlyList<Document>, Task> onDcoumentsChanged, ILogger logger)
        {
            this.feedName = feedName;
            this.onDocumentsChanged = onDcoumentsChanged;
            this.logger = logger;
        }

        public Task CloseAsync(IChangeFeedObserverContext context, ChangeFeedObserverCloseReason reason)
        {
            using (this.logger.BeginSingleScope(
                (LoggerScopeHelpers.MethodScope, MethodClose)))
            {
                this.logger.LogDebug($"feed:{this.feedName} reason:{reason}");
            }

            return Task.CompletedTask;
        }

        public Task OpenAsync(IChangeFeedObserverContext context)
        {
            using (this.logger.BeginSingleScope(
                (LoggerScopeHelpers.MethodScope, MethodOpen)))
            {
                this.logger.LogDebug($"feed:{this.feedName}");
            }

            return Task.CompletedTask;
        }

        public Task ProcessChangesAsync(IChangeFeedObserverContext context, IReadOnlyList<Document> docs, CancellationToken cancellationToken)
        {
            return this.onDocumentsChanged(docs);
        }
    }

#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore SA1649 // File name should match first type name
}
