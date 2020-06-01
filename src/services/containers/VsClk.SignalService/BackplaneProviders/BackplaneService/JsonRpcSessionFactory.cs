// <copyright file="JsonRpcSessionFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

            EventHandler<JsonRpcDisconnectedEventArgs> disconnectHandler = null;
            disconnectHandler = (s, e) =>
            {
                jsonRpc.Disconnected -= disconnectHandler;

                BackplaneService.OnDisconnected(null, e.Exception);
                this.rpcSessions.TryRemove(jsonRpc);
                Logger.LogInformation($"rpc disconnected -> service type:{ServiceType} connections:{this.rpcSessions.Count}");
            };
            jsonRpc.Disconnected += disconnectHandler;
        }

        /// <summary>
        /// Invoke the backplane service.
        /// </summary>
        /// <param name="backplaneServiceFunc">A callback to process.</param>
        /// <param name="methodName">Name of the method to report in case of failure.</param>
        /// <returns>Task completion.</returns>
        protected async Task InvokeBackplaneService(Func<T, Task> backplaneServiceFunc, string methodName)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                await backplaneServiceFunc(BackplaneService);
                BackplaneService.TrackMethodPerf(methodName, sw.Elapsed);
            }
            catch (Exception err)
            {
                Logger.LogError(err, $"Failed to invoke method:{methodName}");
                throw;
            }
        }

        protected async Task<TResult> InvokeBackplaneService<TResult>(Func<T, Task<TResult>> backplaneServiceFunc, string methodName)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                var result = await backplaneServiceFunc(BackplaneService);
                BackplaneService.TrackMethodPerf(methodName, sw.Elapsed);
                return result;
            }
            catch (Exception err)
            {
                Logger.LogError(err, $"Failed to invoke method:{methodName}");
                throw;
            }
        }

        /// <summary>
        /// Invoke all the connected rpc sessions.
        /// </summary>
        /// <param name="targetName">The remote target name to invoke.</param>
        /// <param name="arguments">Arguments expected on the remote rpc host side.</param>
        /// <returns>Task completion.</returns>
        protected Task InvokeAllAsync(string targetName, params object[] arguments)
        {
            return InvokeOrNotifyAsync(targetName, arguments, true);
        }

        /// <summary>
        /// Notify all the connected rpc sessions.
        /// </summary>
        /// <param name="targetName">The remote target name to notify.</param>
        /// <param name="arguments">Arguments expected on the remote rpc host side.</param>
        /// <returns>Task completion.</returns>
        protected Task NotifyAllAsync(string targetName, params object[] arguments)
        {
            return InvokeOrNotifyAsync(targetName, arguments, false);
        }

        private static bool IsDisconnectException(Exception[] exceptions)
        {
            return exceptions.Any(e =>
                    e is SocketException ||
                    e is ConnectionLostException ||
                    e is OperationCanceledException);
        }

        private async Task InvokeOrNotifyAsync(string targetName, object[] arguments, bool invokeFlag)
        {
            var taskInfos = this.rpcSessions.Values.ToArray().Select(
                rpcSession =>
                (rpcSession, invokeFlag ? rpcSession.InvokeAsync(targetName, arguments) : rpcSession.NotifyAsync(targetName, arguments)));
            try
            {
                await Task.WhenAll(taskInfos.Select(t => t.Item2));
            }
            catch (Exception)
            {
                // each task exception will be logged next
            }

            int rpcSessionNumber = 0;
            foreach (var taskInfo in taskInfos)
            {
                var exception = taskInfo.Item2.Exception;

                // Note: don't log if the rpc session was just disconnected after we snapshot the list
                // and so the rpc session was already removed
                if (exception != null && this.rpcSessions.Contains(taskInfo.Item1) && !taskInfo.Item1.IsDisposed)
                {
                    Logger.LogError(exception, $"Failed to notify->targetName:{targetName} service type:{ServiceType} rpc#:{rpcSessionNumber}");
                    if (IsDisconnectException(exception.GetInnerExceptions()))
                    {
                        Logger.LogWarning($"rpc session disposed rpc#:{rpcSessionNumber}");
                        this.rpcSessions.TryRemove(taskInfo.Item1);
                        taskInfo.Item1.Dispose();
                    }
                }

                ++rpcSessionNumber;
            }
        }
    }
}
