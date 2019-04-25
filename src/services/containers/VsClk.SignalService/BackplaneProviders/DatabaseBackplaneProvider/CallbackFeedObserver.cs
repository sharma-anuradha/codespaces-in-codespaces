using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor.FeedProcessing;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService
{
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
        private readonly string feedName;
        private readonly Func<IReadOnlyList<Document>, Task> onDocumentsChanged;
        private readonly ILogger logger;

        public CallbackFeedObserver(string feedName,Func<IReadOnlyList<Document>, Task> onDcoumentsChanged, ILogger logger)
        {
            this.feedName = feedName;
            this.onDocumentsChanged = onDcoumentsChanged;
            this.logger = logger;
        }

        public Task CloseAsync(IChangeFeedObserverContext context, ChangeFeedObserverCloseReason reason)
        {
            this.logger.LogDebug($"CallbackFeedObserver.CloseAsync -> feed:{this.feedName}");
            return Task.CompletedTask;
        }

        public Task OpenAsync(IChangeFeedObserverContext context)
        {
            this.logger.LogDebug($"CallbackFeedObserver.OpenAsync -> feed:{this.feedName}");
            return Task.CompletedTask;
        }

        public Task ProcessChangesAsync(IChangeFeedObserverContext context, IReadOnlyList<Document> docs, CancellationToken cancellationToken)
        {
            return this.onDocumentsChanged(docs);
        }
    }
}
