// <copyright file="DataflowExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Extensions for the TPL library.
    /// </summary>
    public static class DataflowExtensions
    {
        public static readonly ExecutionDataflowBlockOptions DefaultDataflowBlockOptions = new ExecutionDataflowBlockOptions()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
        };

        /// <summary>
        /// Run an action on list of inputs
        /// </summary>
        /// <typeparam name="TInput">Type of the input to process.</typeparam>
        /// <param name="inputs">List of inputs.</param>
        /// <param name="inputActionCallback">Action to run.</param>
        /// <param name="errItemCallback">Exception callback for an item.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="cancellationToken">Optional canceelation token.</param>
        /// <returns>Completion task.</returns>
        public static Task RunConcurrentItemsAsync<TInput>(
            this IEnumerable<TInput> inputs,
            Func<TInput, IDiagnosticsLogger, CancellationToken, Task> inputActionCallback,
            Func<TInput, Exception, IDiagnosticsLogger, CancellationToken, Task> errItemCallback,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken)
        {
            return RunConcurrentItemsAsync(inputs, inputActionCallback, errItemCallback, DefaultDataflowBlockOptions, logger, cancellationToken);
        }

        /// <summary>
        /// Run an action on list of inputs
        /// </summary>
        /// <typeparam name="TInput">Type of the input to process.</typeparam>
        /// <param name="inputs">List of inputs.</param>
        /// <param name="inputActionCallback">Action to run.</param>
        /// <param name="errItemCallback">Exception callback for an item.</param>
        /// <param name="executionDataflowBlockOptions">Dataflow block options.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="cancellationToken">Optional canceelation token.</param>
        /// <returns>Completion task.</returns>
        public static Task RunConcurrentItemsAsync<TInput>(
            this IEnumerable<TInput> inputs,
            Func<TInput, IDiagnosticsLogger, CancellationToken, Task> inputActionCallback,
            Func<TInput, Exception, IDiagnosticsLogger, CancellationToken, Task> errItemCallback,
            ExecutionDataflowBlockOptions executionDataflowBlockOptions,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken)
        {
            Requires.NotNull(inputs, nameof(inputs));

            return RunConcurrentAsync(
                async (inputBlock, ct) =>
                {
                    foreach (var input in inputs)
                    {
                        await inputBlock.SendAsync(input, ct);
                    }
                },
                inputActionCallback,
                errItemCallback,
                executionDataflowBlockOptions,
                logger,
                cancellationToken);
        }

        /// <summary>
        /// Process items produced by a callback.
        /// </summary>
        /// <typeparam name="TInput">Type of the input to process.</typeparam>
        /// <param name="inputProducerCallback">The input producer callback.</param>
        /// <param name="inputActionCallback">Action to run.</param>
        /// <param name="errItemCallback">Exception callback for an item.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="cancellationToken">Optional canceelation token.</param>
        /// <returns>Completion task.</returns>
        public static Task RunConcurrentAsync<TInput>(
            Func<ITargetBlock<TInput>, CancellationToken, Task> inputProducerCallback,
            Func<TInput, IDiagnosticsLogger, CancellationToken, Task> inputActionCallback,
            Func<TInput, Exception, IDiagnosticsLogger, CancellationToken, Task> errItemCallback,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken)
        {
            return RunConcurrentAsync(
                inputProducerCallback,
                inputActionCallback,
                errItemCallback,
                DefaultDataflowBlockOptions,
                logger,
                cancellationToken);
        }

        /// <summary>
        /// Process items produced by a callback.
        /// </summary>
        /// <typeparam name="TInput">Type of the input to process.</typeparam>
        /// <param name="inputProducerCallback">The input producer callback.</param>
        /// <param name="inputActionCallback">Action to run.</param>
        /// <param name="errItemCallback">Exception callback for an item.</param>
        /// <param name="executionDataflowBlockOptions">Dataflow block options.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="cancellationToken">Optional canceelation token.</param>
        /// <returns>Completion task.</returns>
        public static async Task RunConcurrentAsync<TInput>(
            Func<ITargetBlock<TInput>, CancellationToken, Task> inputProducerCallback,
            Func<TInput, IDiagnosticsLogger, CancellationToken, Task> inputActionCallback,
            Func<TInput, Exception, IDiagnosticsLogger, CancellationToken, Task> errItemCallback,
            ExecutionDataflowBlockOptions executionDataflowBlockOptions,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken)
        {
            Requires.NotNull(inputProducerCallback, nameof(inputProducerCallback));
            Requires.NotNull(inputActionCallback, nameof(inputActionCallback));
            Requires.NotNull(executionDataflowBlockOptions, nameof(executionDataflowBlockOptions));
            Requires.NotNull(logger, nameof(logger));

            var index = 0;

            var exceptions = new ConcurrentBag<Exception>();
            var inputBlock = new TransformBlock<TInput, Exception>(
                item =>
                {
                    var itemIndex = Interlocked.Increment(ref index);

                    return logger.OperationScopeAsync(
                        "run_concurrent_item",
                        async (childLogger) =>
                        {
                            childLogger.FluentAddBaseValue("TaskItemRunId", Guid.NewGuid())
                                .FluentAddValue("IterateItemIndex", itemIndex);

                            await inputActionCallback(item, childLogger, cancellationToken);
                            return null;
                        },
                        errCallback: async (error, childLogger) =>
                        {
                            exceptions.Add(error);
                            if (errItemCallback != null)
                            {
                                await errItemCallback(item, error, childLogger, cancellationToken);
                            }

                            return error;
                        },
                        swallowException: true);
                },
                executionDataflowBlockOptions);

            var resultErrors = new BufferBlock<Exception>();
            inputBlock.LinkTo(resultErrors);

            async Task SendItemsAsync()
            {
                await inputProducerCallback(inputBlock, cancellationToken);
                inputBlock.Complete();
            }

            // start the dataflow
            await Task.WhenAll(SendItemsAsync(), inputBlock.Completion);
            resultErrors.Complete();

            // Throw if we had exceptions
            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }
    }
}
