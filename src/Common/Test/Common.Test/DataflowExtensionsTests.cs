using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test
{
    public class DataflowExtensionsTests
    {
        private readonly static TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(60);

        [Fact]
        public async Task RunConcurrentAsync()
        {
            var valueItems = new ConcurrentBag<int>();
            var processed = new BufferBlock<int>();
            var tcs = new TaskCompletionSource<bool>();
            var task = DataflowExtensions.RunConcurrentAsync<int>(
                async (target, ct) =>
                {
                    await target.SendAsync(10, ct);
                    await target.SendAsync(20, ct);
                    await tcs.Task;
                    await target.SendAsync(30, ct);
                },
                async (value, logger, ct) =>
                {
                    valueItems.Add(value);
                    await processed.SendAsync(value);
                },
                errItemCallback: null,
                new NullLogger(),
                default);

            // first 2 items should be queued and processed.
            await processed.ReceiveAsync(ReceiveTimeout);
            await processed.ReceiveAsync(ReceiveTimeout);
            // unblock items
            tcs.SetResult(true);
            // now concurrency should be finished.
            await task;
            await processed.ReceiveAsync(ReceiveTimeout);
            Assert.Contains(10, valueItems);
            Assert.Contains(20, valueItems);
            Assert.Contains(30, valueItems);
        }

        [Fact]
        public async Task WaitAllItemsAsync()
        {
            var processed = new BufferBlock<int>();
            var tcs = new TaskCompletionSource<bool>();
            var task = DataflowExtensions.RunConcurrentAsync<int>(
                async (target, ct) =>
                {
                    await target.SendAsync(10, ct);
                },
                async (value, logger, ct) =>
                {
                    await tcs.Task;
                    await processed.SendAsync(value);
                },
                errItemCallback: null,
                new NullLogger(),
                default);
            var taskCompleted = await Task.WhenAny(task, Task.Delay(TimeSpan.FromMilliseconds(500)));
            // Note: task should not be completed.
            Assert.NotEqual(taskCompleted, task);
            tcs.SetResult(true);
            await task;
            var item = await processed.ReceiveAsync(ReceiveTimeout);
            Assert.Equal(10, item);
        }

        [Fact]
        public async Task RunConcurrentItemsAsync()
        {
            var processed = new BufferBlock<int>();
            var valueItems = new ConcurrentBag<int>();
            var inputs = new int[] { 10, 20, 30, 40, 50 };
            await inputs.RunConcurrentItemsAsync(
                (value, logger, ct) =>
                {
                    valueItems.Add(value);
                    return processed.SendAsync(value);
                },
                errItemCallback: null,
                new NullLogger(),
                default);

            async Task Receive(BufferBlock<int> buffer,int? count = null)
            {
                for (int i = 0; i < (count.HasValue ? count.Value : inputs.Length); ++i)
                {
                    await buffer.ReceiveAsync(ReceiveTimeout);
                }
            }


            await Receive(processed);

            foreach (var value in valueItems)
            {
                Assert.Contains(value, valueItems);
            }

            var waiting = new BufferBlock<int>();
            var tcs = new TaskCompletionSource<bool>();
            // Note: we can't aswait here since we are blocking every item on the tcs entity
            var task = inputs.RunConcurrentItemsAsync(
                async (value, logger, ct) =>
                {
                    await waiting.SendAsync(value);
                    await tcs.Task;
                    await processed.SendAsync(value);
                },
                errItemCallback: null,
                new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = inputs.Length },
                new NullLogger(),
                default);

            await Receive(waiting);
            // unblock all items
            tcs.SetResult(true);
            // now concurrency should be finished.
            await task;
            await Receive(processed);

            var errItems = new BufferBlock<int>();
            var exception = await Assert.ThrowsAsync<AggregateException>(() => inputs.RunConcurrentItemsAsync(
                (value, logger, ct) =>
                {
                    if (value == 30)
                    {
                        throw new ArgumentException();
                    }

                    return processed.SendAsync(value);
                },
                errItemCallback: (value, error, logger, ct) =>
                {
                    return errItems.SendAsync(value);
                },
                new NullLogger(),
                default));

            await Receive(processed, inputs.Length - 1);
            await Receive(errItems, 1);
        }
    }
}
