// <copyright file="JsonRpcSessionFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VsCloudKernel.SignalService;
using Microsoft.VsCloudKernel.SignalService.Common;
using StreamJsonRpc;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// Json rpc session factory base class.
    /// </summary>
    /// <typeparam name="T">The backplane manager service type.</typeparam>
    /// <typeparam name="TBackplaneManagerType">The backplane manager type.</typeparam>
    /// <typeparam name="TNotify">The notify type.</typeparam>
    public abstract class JsonRpcSessionFactory<T, TBackplaneManagerType, TNotify> : IJsonRpcSessionFactory
        where T : BackplaneService<TBackplaneManagerType, TNotify>
        where TBackplaneManagerType : class, IBackplaneManagerBase
        where TNotify : class
    {
        private readonly ConcurrentHashSet<JsonRpc> rpcSessions = new ConcurrentHashSet<JsonRpc>();

        protected JsonRpcSessionFactory(T backplaneService, ILogger logger)
        {
            BackplaneService = Requires.NotNull(backplaneService, nameof(backplaneService));
            Logger = Requires.NotNull(logger, nameof(logger));
            backplaneService.AddBackplaneServiceNotification(this as TNotify);
        }

        /// <summary>
        /// Gets the service type being served.
        /// </summary>
        public abstract string ServiceType { get; }

        protected ILogger Logger { get; }

        protected T BackplaneService { get; }

        /// <inheritdoc/>
        public void StartRpcSession(JsonRpc jsonRpc, string serviceId)
        {
            BackplaneService.RegisterService(serviceId);

            jsonRpc.AddLocalRpcTarget(this);
            this.rpcSessions.Add(jsonRpc);

            Logger.LogMethodScope(LogLevel.Information, $"connected -> service type:{ServiceType} connections:{this.rpcSessions.Count}", nameof(StartRpcSession));

            jsonRpc.Disconnected += (s, e) =>
            {
                BackplaneService.OnDisconnected(null, e.Exception);
                this.rpcSessions.TryRemove(jsonRpc);
                Logger.LogInformation($"rpc disonnected -> service type:{ServiceType} connections:{this.rpcSessions.Count}");
            };
        }

        /// <summary>
        /// Invoke all the connected rpc sessions.
        /// </summary>
        /// <param name="targetName">The remote target name to invoke.</param>
        /// <param name="arguments">Arguments expected on the remote rpc host side.</param>
        /// <returns>Task completion.</returns>
        protected async Task InvokeAllAsync(string targetName, params object[] arguments)
        {
            foreach (var jsonRpc in this.rpcSessions.Values.ToArray())
            {
                try
                {
                    await jsonRpc.InvokeAsync(targetName, arguments);
                }
                catch (Exception err)
                {
                    Logger.LogError(err, $"Failed to notify->targetName:{targetName}");
                }
            }
        }
    }
}
