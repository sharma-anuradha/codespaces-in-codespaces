using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Test
{
    public class JobQueueTest : QueueTestBase
    {
        private readonly static TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(60);

        public JobQueueTest()
#if false
            : base(StorageQueueFactoryMock.Create())
#else
            : base(new BufferBlockQueueFactory())
#endif
        {
        }

        [Fact]
        public Task SingleConsumerAsync()
        {
            return RunJobQueueTest(async (jobQueueProducer, jobQueueConsumer, queue) =>
            {
                var payloadsProcessed1 = new BufferBlock<JobContentPayload<int>>();
                var payloadsProcessed2 = new BufferBlock<JobContentPayload<string>>();

                jobQueueConsumer.RegisterJobPayloadHandler<JobContentPayload<int>>(
                    async (payload, logger, ct) =>
                    {
                        await payloadsProcessed1.SendAsync(payload);
                    });
                jobQueueConsumer.RegisterJobPayloadHandler<JobContentPayload<string>>(
                    async (payload, logger, ct) =>
                    {
                        await payloadsProcessed2.SendAsync(payload);
                    });

                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(100));
                await jobQueueProducer.AddJobAsync(new JobContentPayload<string>("hi"));
                var payloadReceived1 = await payloadsProcessed1.ReceiveAsync(ReceiveTimeout);
                Assert.Equal(100, payloadReceived1.Content);
                var payloadReceived2 = await payloadsProcessed2.ReceiveAsync(ReceiveTimeout);
                Assert.Equal("hi", payloadReceived2.Content);
            });
        }

        [Fact]
        public Task JobPayloadErrorAsync()
        {
            return RunJobQueueTest(async (jobQueueProducer, jobQueueConsumer, queue) =>
            {
                var payloadsProcessed = new BufferBlock<JobPayloadError>();
                jobQueueConsumer.RegisterJobPayloadHandler<JobPayloadError>(
                    async (payload, logger, ct) =>
                    {
                        await payloadsProcessed.SendAsync(payload);
                    });
                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(100));
                var jobPayloadError = await payloadsProcessed.ReceiveAsync(ReceiveTimeout);
                Assert.IsType<NotSupportedException>(jobPayloadError.Error);
            });
        }

        [Fact]
        public Task JobPayloadHandlerTimeoutAsync()
        {
            return RunJobQueueTest(async (jobQueueProducer, jobQueueConsumer, queue) =>
            {
                var payloadsProcessed = new BufferBlock<JobContentPayload<int>>();
                jobQueueConsumer.RegisterJobPayloadHandler<JobContentPayload<int>>(
                    async (payload, logger, ct) =>
                    {
                        await Task.Delay(payload.Content, ct);
                        await payloadsProcessed.SendAsync(payload);
                    });
                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(200), new JobPayloadOptions() { HandlerTimout = TimeSpan.FromMilliseconds(50) });
                await Assert.ThrowsAsync<TimeoutException>(() => payloadsProcessed.ReceiveAsync(TimeSpan.FromMilliseconds(100)));
            });
        }

        [Fact]
        public Task JobPayloadHandlerMaxRetriestAsync()
        {
            return RunJobQueueTest(async (jobQueueProducer, jobQueueConsumer, queue) =>
            {
                var retries = 0;
                var payloadsProcessed = new BufferBlock<JobContentPayload<int>>();
                jobQueueConsumer.RegisterJobPayloadHandler<JobContentPayload<int>>(
                    (payload, logger, ct) =>
                    {
                        ++retries;
                        throw new NotSupportedException();
                    });
                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(-1), new JobPayloadOptions() { MaxHandlerRetries = 2 });
                await Task.Delay(TimeSpan.FromSeconds(5));
                Assert.Equal(2, retries);
            }, new QueueMessageProducerSettings(5, TimeSpan.FromSeconds(1), QueueMessageProducerSettings.DefaultTimeout));
        }

        [Fact]
        public Task JobPayloadInvisibleTimeoutDelayAsync()
        {
            return RunJobQueueTest(async (jobQueueProducer, jobQueueConsumer, queue) =>
            {
                var payloadsProcessed = new BufferBlock<JobContentPayload<int>>();
                jobQueueConsumer.RegisterJobPayloadHandler<JobContentPayload<int>>(
                    async (payload, logger, ct) =>
                    {
                        await payloadsProcessed.SendAsync(payload);
                    });
                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(-1), new JobPayloadOptions() { InitialVisibilityDelay = TimeSpan.FromMilliseconds(100) });
                await Assert.ThrowsAsync<TimeoutException>(() => payloadsProcessed.ReceiveAsync(TimeSpan.FromMilliseconds(50)));
                var payloadReceived = await payloadsProcessed.ReceiveAsync(ReceiveTimeout);
                Assert.Equal(-1, payloadReceived.Content);
            });
        }

        [Fact]
        public Task JobHandlerExceptionAsync()
        {
            return RunJobQueueTest(async (jobQueueProducer, jobQueueConsumer, queue) =>
            {
                var payloadsProcessed = new BufferBlock<JobContentPayload<int>>();

                var throwOnData = 50;
                var jobRetries = 0;

                jobQueueConsumer.RegisterJobHandler<JobContentPayload<int>>(
                    async (job, logger, ct) =>
                    {
                        if (job.Payload.Content == throwOnData)
                        {
                            throw new ArgumentException();
                        }

                        jobRetries = job.Retries;
                        await payloadsProcessed.SendAsync(job.Payload);
                    });
                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(100));
                var payloadReceived = await payloadsProcessed.ReceiveAsync(ReceiveTimeout);
                Assert.Equal(100, payloadReceived.Content);

                // this will force an exception on the job handler.
                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(50));

                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(200));
                payloadReceived = await payloadsProcessed.ReceiveAsync(ReceiveTimeout);
                Assert.Equal(200, payloadReceived.Content);
                throwOnData = -1;
                // item should be re posted back on the queue.
                payloadReceived = await payloadsProcessed.ReceiveAsync(ReceiveTimeout);
                Assert.Equal(50, payloadReceived.Content);
                Assert.Equal(1, jobRetries);

            }, new QueueMessageProducerSettings(5, TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(500)));
        }

        [Fact]
        public Task MultipleConsumersAsync()
        {
            return RunJobQueueTest(async (jobQueueProducer, jobQueueConsumer, queue) =>
            {
                var otherQueueMessageProducer = new QueueMessageProducer(queue, QueueMessageProducerSettings.Default);
                var otherJobQueueConsumer = new JobQueueConsumer(otherQueueMessageProducer, new NullLogger());
                var executionDataflowBlockOptions = new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 1
                };

                var payloadsProcessed1Counter = 0;
                var payloadsProcessed2Counter = 0;
                var payloadsProcessed = new BufferBlock<JobContentPayload<int>>();

                jobQueueConsumer.RegisterJobPayloadHandler<JobContentPayload<int>>(
                    async (payload, logger, ct) =>
                    {
                        ++payloadsProcessed1Counter;
                        await payloadsProcessed.SendAsync(payload);
                    }, executionDataflowBlockOptions);
                otherJobQueueConsumer.RegisterJobPayloadHandler<JobContentPayload<int>>(
                    async (payload, logger, ct) =>
                    {
                        ++payloadsProcessed2Counter;
                        await payloadsProcessed.SendAsync(payload);
                    }, executionDataflowBlockOptions);

                for (int i = 0; i < 10; ++i)
                {
                    await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(i));
                }

                for (int i = 0; i < 10; ++i)
                {
                    await payloadsProcessed.ReceiveAsync(ReceiveTimeout);
                }

                Assert.True(payloadsProcessed1Counter > 0);
                Assert.True(payloadsProcessed2Counter > 0);
            });
        }

        [Fact]
        public Task JobHandlerUpdateVisbilityAsync()
        {
            return RunJobQueueTest(async (jobQueueProducer, jobQueueConsumer, queue) =>
            {
                var payloadsProcessed = new BufferBlock<JobContentPayload<int>>();
                jobQueueConsumer.RegisterJobHandler<JobContentPayload<int>>(
                    async (job, logger, ct) =>
                    {
                        await job.UpdateAsync(TimeSpan.FromSeconds(60), ct);
                        await payloadsProcessed.SendAsync(job.Payload);
                    });
                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(200), new JobPayloadOptions() { HandlerTimout = TimeSpan.FromMilliseconds(50) });

                var payloadReceived = await payloadsProcessed.ReceiveAsync(ReceiveTimeout);
                Assert.Equal(200, payloadReceived.Content);
            });
        }

        [Fact]
        public Task GetMetricsAsync()
        {
            return RunJobQueueTest(async (jobQueueProducer, jobQueueConsumer, queue) =>
            {
                var waitHandlers = new BufferBlock<int>();
                var processHandlers = new BufferBlock<int>();

                jobQueueConsumer.RegisterJobHandler<JobContentPayload<int>>(
                    async (job, logger, ct) =>
                    {
                        if (job.Payload.Content == 10)
                        {
                            throw new NotSupportedException();
                        }

                        await waitHandlers.ReceiveAsync(ReceiveTimeout);
                        await processHandlers.SendAsync(0);
                    }, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 });
                var payloadStringHandlers = new BufferBlock<string>();
                jobQueueConsumer.RegisterJobHandler<JobContentPayload<string>>(
                    async (job, logger, ct) =>
                    {
                        await waitHandlers.ReceiveAsync(ReceiveTimeout);
                        await processHandlers.SendAsync(0);
                    }, new ExecutionDataflowBlockOptions() { MaxDegreeOfParallelism = 1 });

                const int TotalIntJosb = 50;
                const int TotalStrJosb = 25;

                for (int i = 0; i < TotalIntJosb; ++i)
                {
                    await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(i));
                }

                for (int i = 0; i < TotalStrJosb; ++i)
                {
                    await jobQueueProducer.AddJobAsync(new JobContentPayload<string>($"next:{i}"));
                }

                for (int i = 0; i < (TotalIntJosb + TotalStrJosb); ++i)
                {
                    await waitHandlers.SendAsync(0);
                }

                for (int i = 0; i < (TotalIntJosb + TotalStrJosb -1); ++i)
                {
                    await processHandlers.ReceiveAsync(ReceiveTimeout);
                }

                var metrics = jobQueueConsumer.GetMetrics();
                Assert.Equal(2, metrics.Count);
                var typeIntTag = JobPayloadHelpers.GetTypeTag(typeof(JobContentPayload<int>));
                Assert.True(metrics.ContainsKey(typeIntTag));
                var jobIntMetrics = metrics[typeIntTag];
                Assert.Equal(TotalIntJosb, jobIntMetrics.Processed);
                Assert.Equal(TotalIntJosb - 1, jobIntMetrics.MaxInputCount);
                Assert.Equal(1, jobIntMetrics.Failures);
                Assert.Equal(0, jobIntMetrics.Cancelled);
                Assert.NotEqual(TimeSpan.Zero, jobIntMetrics.ProcessTime);
            });
        }

        protected Task RunJobQueueTest(Func<JobQueueProducer, JobQueueConsumer, IQueue, Task> testCallback,
            QueueMessageProducerSettings queueMessageProducerSettings = null)
        {
            return RunQueueTest(async (queue) =>
            {
                var jobQueueProducer = new JobQueueProducer(queue, new NullLogger());
                var queueMessageProducer = new QueueMessageProducer(queue, queueMessageProducerSettings ?? QueueMessageProducerSettings.Default);
                var jobQueueConsumer = new JobQueueConsumer(queueMessageProducer, new NullLogger());
                await testCallback(jobQueueProducer, jobQueueConsumer, queue);
                await jobQueueProducer.DisposeAsync();
                await jobQueueConsumer.DisposeAsync();
                await queueMessageProducer.DisposeAsync();
            });
        }
    }
}
