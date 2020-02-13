// <copyright file="BackplaneService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// Backplane service base class.
    /// </summary>
    /// <typeparam name="TNotify">The notification type</typeparam>
    public class BackplaneService<TBackplaneManagerType, TNotify>
        where TBackplaneManagerType : class
    {
        private readonly CancellationTokenSource disposeTokenSource = new CancellationTokenSource();
        private readonly List<TNotify> backplaneServiceNotifications = new List<TNotify>();
        private int numOfConnections;

        protected BackplaneService(TBackplaneManagerType backplaneManager, IEnumerable<TNotify> notifications, ILogger logger)
        {
            BackplaneManager = Requires.NotNull(backplaneManager, nameof(backplaneManager));
            this.backplaneServiceNotifications.AddRange(notifications);

            Logger = Requires.NotNull(logger, nameof(logger));
        }

        protected CancellationToken DisposeToken => this.disposeTokenSource.Token;

        protected TBackplaneManagerType BackplaneManager { get; }

        protected ILogger Logger { get; }

        protected IEnumerable<TNotify> BackplaneServiceNotifications => this.backplaneServiceNotifications;

        public void AddBackplaneServiceNotification(TNotify backplaneServiceNotification)
        {
            this.backplaneServiceNotifications.Add(backplaneServiceNotification);
        }

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
    }
}
