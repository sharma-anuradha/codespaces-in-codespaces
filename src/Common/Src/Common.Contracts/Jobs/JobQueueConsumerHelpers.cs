// <copyright file="JobQueueConsumerHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Helper extension for a job queue consumer.
    /// </summary>
    public static class JobQueueConsumerHelpers
    {
        private static readonly Lazy<MethodInfo> LazyRegisterHandlerMethod = new Lazy<MethodInfo>(() =>
        {
            return typeof(IJobQueueConsumer).GetMethods().Where(m => m.Name == nameof(IJobQueueConsumer.RegisterJobHandler) && m.IsGenericMethod).FirstOrDefault();
        });

        /// <summary>
        /// Register a job handler by the common interface.
        /// </summary>
        /// <param name="jobQueueConsumer">The job consumer instance.</param>
        /// <param name="jobHandler">The job handler to regsiter.</param>
        public static void RegisterJobHandler(this IJobQueueConsumer jobQueueConsumer, IJobHandler jobHandler)
        {
            var jobHandlerGenericType = jobHandler.GetType().GetInterfaces().Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IJobHandler<>)).FirstOrDefault();
            if (jobHandlerGenericType == null)
            {
                throw new ArgumentException(nameof(jobHandler));
            }

            var method = LazyRegisterHandlerMethod.Value.MakeGenericMethod(jobHandlerGenericType.GetGenericArguments());
            method.Invoke(jobQueueConsumer, new object[] { jobHandler, GetDataflowBlockOptions(jobHandler), GetJobHandlerOptions(jobHandler) });
        }

        /// <summary>
        /// Register multiple job handlers.
        /// </summary>
        /// <param name="jobQueueConsumer">The job consumer instance.</param>
        /// <param name="jobHandlers">Enumerable job handlers to regsiter.</param>
        public static void RegisterJobHandlers(this IJobQueueConsumer jobQueueConsumer, IEnumerable<IJobHandler> jobHandlers)
        {
            foreach (var jobHandler in jobHandlers)
            {
                RegisterJobHandler(jobQueueConsumer, jobHandler);
            }
        }

        /// <summary>
        /// Register a job handler with dataflow options.
        /// </summary>
        /// <typeparam name="T">Type of the payload of the job handler.</typeparam>
        /// <param name="jobQueueConsumer">The job consumer instance.</param>
        /// <param name="jobHandler">The job handler to regsiter.</param>
        public static void RegisterJobHandler<T>(this IJobQueueConsumer jobQueueConsumer, IJobHandler<T> jobHandler)
            where T : JobPayload
        {
            jobQueueConsumer.RegisterJobHandler<T>(jobHandler, GetDataflowBlockOptions(jobHandler));
        }

        /// <summary>
        /// Register a job handler with a callback that use the job payload.
        /// </summary>
        /// <typeparam name="T">Type of the payload of the job handler.</typeparam>
        /// <param name="jobQueueConsumer">The job consumer instance.</param>
        /// <param name="handleJobCallback">The delegate callback.</param>
        /// <param name="dataflowBlockOptions">Optional dataflow block options.</param>
        /// <param name="options">Job handler options.</param>
        public static void RegisterJobPayloadHandler<T>(
            this IJobQueueConsumer jobQueueConsumer,
            Func<T, IDiagnosticsLogger, CancellationToken, Task> handleJobCallback,
            ExecutionDataflowBlockOptions dataflowBlockOptions = null,
            JobHandlerOptions options = null)
            where T : JobPayload
        {
            jobQueueConsumer.RegisterJobHandler(CreateJobPayloadHandler(handleJobCallback, dataflowBlockOptions, options));
        }

        /// <summary>
        /// Register a job handler with a callback that use the the job reference.
        /// </summary>
        /// <typeparam name="T">Type of the payload of the job handler.</typeparam>
        /// <param name="jobQueueConsumer">The job consumer instance.</param>
        /// <param name="handleJobCallback">The delegate callback.</param>
        /// <param name="dataflowBlockOptions">Optional dataflow block options.</param>
        /// <param name="options">Job handler options.</param>
        public static void RegisterJobHandler<T>(
            this IJobQueueConsumer jobQueueConsumer,
            Func<IJob<T>, IDiagnosticsLogger, CancellationToken, Task> handleJobCallback,
            ExecutionDataflowBlockOptions dataflowBlockOptions = null,
            JobHandlerOptions options = null)
            where T : JobPayload
        {
            jobQueueConsumer.RegisterJobHandler(CreateJobHandler(handleJobCallback, dataflowBlockOptions, options));
        }

        /// <summary>
        /// Create a job handler instance based on a delegate callback.
        /// </summary>
        /// <typeparam name="T">Type of the paytload to support.</typeparam>
        /// <param name="handleJobCallback">The delegate callback.</param>
        /// <param name="dataflowBlockOptions">Optional dataflow block options.</param>
        /// <param name="options">Job handler options.</param>
        /// <returns>A job handler instance.</returns>
        public static IJobHandler<T> CreateJobPayloadHandler<T>(
            Func<T, IDiagnosticsLogger, CancellationToken, Task> handleJobCallback,
            ExecutionDataflowBlockOptions dataflowBlockOptions = null,
            JobHandlerOptions options = null)
            where T : JobPayload
        {
            return new JobHandlerPayload<T>(handleJobCallback, dataflowBlockOptions, options);
        }

        /// <summary>
        /// Create a job handler instance based on a delegate callback.
        /// </summary>
        /// <typeparam name="T">Type of the paytload to support.</typeparam>
        /// <param name="handleJobCallback">The delegate callback.</param>
        /// <param name="dataflowBlockOptions">Optional dataflow block options.</param>
        /// <param name="options">Job handler options.</param>
        /// <returns>A job handler instance.</returns>
        public static IJobHandler<T> CreateJobHandler<T>(Func<IJob<T>, IDiagnosticsLogger, CancellationToken, Task> handleJobCallback, ExecutionDataflowBlockOptions dataflowBlockOptions = null, JobHandlerOptions options = null)
            where T : JobPayload
        {
            return new JobHandler<T>(handleJobCallback, dataflowBlockOptions, options);
        }

        private static ExecutionDataflowBlockOptions GetDataflowBlockOptions(IJobHandler jobHandler)
        {
            if (jobHandler is IJobHandlerOptions handlerOptions)
            {
                return handlerOptions.DataflowBlockOptions;
            }

            return new ExecutionDataflowBlockOptions();
        }

        private static JobHandlerOptions GetJobHandlerOptions(IJobHandler jobHandler)
        {
            if (jobHandler is IJobHandlerOptions handlerOptions)
            {
                return handlerOptions.Options;
            }

            return null;
        }

        private class JobHandler<T> : JobHandlerBase<T>
            where T : JobPayload
        {
            private readonly Func<IJob<T>, IDiagnosticsLogger, CancellationToken, Task> handleJobCallback;

            public JobHandler(Func<IJob<T>, IDiagnosticsLogger, CancellationToken, Task> handleJobCallback, ExecutionDataflowBlockOptions dataflowBlockOptions, JobHandlerOptions options)
                : base(dataflowBlockOptions, options)
            {
                this.handleJobCallback = handleJobCallback;
            }

            protected override Task HandleJobInternalAsync(IJob<T> job, IDiagnosticsLogger logger, CancellationToken cancellationToken)
            {
                return this.handleJobCallback(job, logger, cancellationToken);
            }
        }

        private class JobHandlerPayload<T> : JobHandlerPayloadBase<T>
            where T : JobPayload
        {
            private readonly Func<T, IDiagnosticsLogger, CancellationToken, Task> handleJobCallback;

            public JobHandlerPayload(Func<T, IDiagnosticsLogger, CancellationToken, Task> handleJobCallback, ExecutionDataflowBlockOptions dataflowBlockOptions, JobHandlerOptions options)
                : base(dataflowBlockOptions, options)
            {
                this.handleJobCallback = handleJobCallback;
            }

            protected override Task HandleJobAsync(T payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
            {
                return this.handleJobCallback(payload, logger, cancellationToken);
            }
        }
    }
}
