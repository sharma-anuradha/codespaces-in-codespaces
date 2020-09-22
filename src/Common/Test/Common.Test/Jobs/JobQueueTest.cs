using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Moq;
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

                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(100), null, NullLogger);
                await jobQueueProducer.AddJobAsync(new JobContentPayload<string>("hi"), null, NullLogger);
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
                var jobsCompleted = new BufferBlock<IJobCompleted>();
                jobQueueConsumer.JobCreated += (job) =>
                {
                    job.Completed += (jobCompleted, ct) =>
                    {
                        return jobsCompleted.SendAsync(jobCompleted, ct);
                    };
                };

                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(100), null, NullLogger);
                var jobCompleted = await jobsCompleted.ReceiveAsync(ReceiveTimeout);
                Assert.Equal(JobCompletedStatus.Failed | JobCompletedStatus.Removed | JobCompletedStatus.PayloadError, jobCompleted.Status);
                Assert.IsType<NotSupportedException>(jobCompleted.Error);
            });
        }

        [Fact]
        public Task JobPayloadErrorCallbackAsync()
        {
            return RunJobQueueTest(async (jobQueueProducer, jobQueueConsumer, queue) =>
            {
                var jobsCompleted = new BufferBlock<(IJob, IJobCompleted)>();
                jobQueueConsumer.JobCreated += (job) =>
                {
                    job.Completed += (jobCompleted, ct) =>
                    {
                        return jobsCompleted.SendAsync((job, jobCompleted), ct);
                    };
                };

                jobQueueConsumer.RegisterJobHandler<JobContentPayload<int>>(
                    (job, logger, ct) =>
                    {
                        if (job.Payload.Content == 10)
                        {
                            throw new NotImplementedException();
                        }
                        else if (job.Payload.Content == 20)
                        {
                            throw new MyJobHandlerException();
                        }

                        return Task.CompletedTask;

                    },dataflowBlockOptions: new ExecutionDataflowBlockOptions()
                    {
                        MaxDegreeOfParallelism = 1,
                    }, options: new JobHandlerOptions()
                    {
                        MaxHandlerRetries = 1,
                        ErrorCallbacks = new IJobHandlerErrorCallback[] { new MyJobHandlerErrorCallback(2) },
                    });

                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(10), null, NullLogger);
                var jobCompleted = await jobsCompleted.ReceiveAsync(ReceiveTimeout);

                Assert.Equal(JobCompletedStatus.Failed | JobCompletedStatus.Removed | JobCompletedStatus.RetryExhausted, jobCompleted.Item2.Status);
                Assert.IsType<NotImplementedException>(jobCompleted.Item2.Error);
                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(20), null, NullLogger);
                jobCompleted = await jobsCompleted.ReceiveAsync(ReceiveTimeout);
                Assert.Equal(JobCompletedStatus.Failed | JobCompletedStatus.Retry, jobCompleted.Item2.Status);
                Assert.Equal(1, jobCompleted.Item1.Retries);
                jobCompleted = await jobsCompleted.ReceiveAsync(ReceiveTimeout);
                Assert.Equal(JobCompletedStatus.Failed | JobCompletedStatus.Retry, jobCompleted.Item2.Status);
                Assert.Equal(2, jobCompleted.Item1.Retries);
                jobCompleted = await jobsCompleted.ReceiveAsync(ReceiveTimeout);
                Assert.Equal(JobCompletedStatus.Failed | JobCompletedStatus.Removed | JobCompletedStatus.RetryExhausted, jobCompleted.Item2.Status);
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
                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(200), new JobPayloadOptions() { HandlerTimeout = TimeSpan.FromMilliseconds(50) }, NullLogger);
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

                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(-1), new JobPayloadOptions() { MaxHandlerRetries = 2, InvisibleThreshold = TimeSpan.Zero }, NullLogger);
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
                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(-1), new JobPayloadOptions() { InitialVisibilityDelay = TimeSpan.FromMilliseconds(100) }, NullLogger);
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

                var jobPayloadOptions = new JobPayloadOptions() { InvisibleThreshold = TimeSpan.Zero };

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
                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(100), jobPayloadOptions, NullLogger);
                var payloadReceived = await payloadsProcessed.ReceiveAsync(ReceiveTimeout);
                Assert.Equal(100, payloadReceived.Content);

                // this will force an exception on the job handler.
                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(50), jobPayloadOptions, NullLogger);

                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(200), jobPayloadOptions, NullLogger);
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
                var otherJobQueueConsumer = new JobQueueConsumer(queue, new NullLogger());
                var executionDataflowBlockOptions = new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 1
                };

                otherJobQueueConsumer.Start(QueueMessageProducerSettings.Default);

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
                    await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(i), null, NullLogger);
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
        public Task JobHandlerUpdateVisibilityAsync()
        {
            return RunJobQueueTest(async (jobQueueProducer, jobQueueConsumer, queue) =>
            {
                var payloadsProcessed = new BufferBlock<JobContentPayload<int>>();
                jobQueueConsumer.RegisterJobHandler<JobContentPayload<int>>(
                    async (job, logger, ct) =>
                    {
                        await job.UpdateVisibilityAsync(TimeSpan.FromSeconds(60), ct);
                        await payloadsProcessed.SendAsync(job.Payload);
                    });
                await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(200), new JobPayloadOptions() { HandlerTimeout = TimeSpan.FromMilliseconds(50) }, NullLogger);

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
                    await jobQueueProducer.AddJobAsync(new JobContentPayload<int>(i), null, NullLogger);
                }

                for (int i = 0; i < TotalStrJosb; ++i)
                {
                    await jobQueueProducer.AddJobAsync(new JobContentPayload<string>($"next:{i}"), null, NullLogger);
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

        [Fact]
        public Task JobLoggerPropertiesAsync()
        {
            return RunJobQueueTest(async (jobQueueProducer, jobQueueConsumer, queue) =>
            {
                var payloadsProcessed = new BufferBlock<JobContentPayload<int>>();
                jobQueueConsumer.RegisterJobPayloadHandler<JobContentPayload<int>>(
                    async (payload, logger, ct) =>
                    {
                        await payloadsProcessed.SendAsync(payload);
                    });
                var jobPayload = new JobContentPayload<int>(100);
                jobPayload.LoggerProperties.Add("property1", 200);
                jobPayload.LoggerProperties.Add("property2", "hi");

                await jobQueueProducer.AddJobAsync(jobPayload, null, NullLogger);
                var jobPayloadReceived = await payloadsProcessed.ReceiveAsync(ReceiveTimeout);
                Assert.Equal(2, jobPayloadReceived.LoggerProperties.Count);
                Assert.Equal(200, Convert.ToInt32(jobPayloadReceived.LoggerProperties["property1"]));
                Assert.Equal("hi", jobPayloadReceived.LoggerProperties["property2"]);
            });
        }

        [Fact]
        public Task ContinuationPayloadAsync()
        {
            return RunJobQueueFactoryTest(async (jobQueueProducerFactory, jobQueueConsumerFactory, queue) =>
            {
                var jobQueueConsumer = jobQueueConsumerFactory.GetOrCreate(queue.Id, null);
                jobQueueConsumer.Start(QueueMessageProducerSettings.Default);
                var continuationJobHandler = new ContinuationJobHandler(jobQueueProducerFactory);
                jobQueueConsumer.RegisterJobHandler(continuationJobHandler);

                var continuationJobPayloadResult = new BufferBlock<ContinuationJobPayloadResult<ContinuationState, ContinuationResult>>();
                jobQueueConsumer.RegisterJobPayloadHandler<ContinuationJobPayloadResult<ContinuationState, ContinuationResult>>(
                    async (payload, logger, ct) =>
                    {
                        await continuationJobPayloadResult.SendAsync(payload);
                    });

                await jobQueueProducerFactory.GetOrCreate(queue.Id, null).AddJobAsync(new ContinuationPayload(), null, NullLogger, default);
                var jobPayloadResult = await continuationJobPayloadResult.ReceiveAsync(ReceiveTimeout);
                Assert.Equal("hello", jobPayloadResult.Result.ResultStep1);
                Assert.Equal(100, jobPayloadResult.Result.ResultStep2);
                Assert.Equal(ContinuationState.Step3, jobPayloadResult.CurrentState);
                Assert.Equal(ContinuationJobPayloadResultState.Succeeded, jobPayloadResult.CompletionState);

                await jobQueueProducerFactory.GetOrCreate(queue.Id, null).AddJobAsync(new ContinuationPayload() { InitValue = 100 } , null, NullLogger, default);
                jobPayloadResult = await continuationJobPayloadResult.ReceiveAsync(ReceiveTimeout);
                Assert.Equal(ContinuationJobPayloadResultState.Failed, jobPayloadResult.CompletionState);
                Assert.Equal(ContinuationState.Step2, jobPayloadResult.CurrentState);

                continuationJobHandler.JobHandlerOptions = new JobHandlerOptions() { MaxHandlerRetries = 10, RetryTimeout = TimeSpan.FromMilliseconds(100) };
                continuationJobHandler.Step1Retries = 4;
                await jobQueueProducerFactory.GetOrCreate(queue.Id, null).AddJobAsync(new ContinuationPayload() { InitValue = 500 }, null, NullLogger, default);
                jobPayloadResult = await continuationJobPayloadResult.ReceiveAsync(ReceiveTimeout);
                Assert.Equal(ContinuationJobPayloadResultState.Succeeded, jobPayloadResult.CompletionState);
                Assert.Equal(0, continuationJobHandler.Step1Retries);
            });
        }

        [Fact]
        public Task DoubleProcessAsync()
        {
            return RunJobQueueTest(async (jobQueueProducer, jobQueueConsumer, queue) =>
            {
                int count = 0;
                var payloadsProcessed = new BufferBlock<JobContentPayload<int>>();
                jobQueueConsumer.RegisterJobPayloadHandler<JobContentPayload<int>>(
                    async (payload, logger, ct) =>
                    {
                        ++count;
                        if (count == 1)
                        {
                            await Task.Delay(10000); // 10 secs
                        }
                        await payloadsProcessed.SendAsync(payload);
                    }, JobHandlerBase.NoParallelismDataflowBlockOptions);
                var jobPayload = new JobContentPayload<int>(100);
                await jobQueueProducer.AddJobAsync(jobPayload, null, NullLogger);
                await jobQueueProducer.AddJobAsync(jobPayload, null, NullLogger);
                await payloadsProcessed.ReceiveAsync(ReceiveTimeout);
                await payloadsProcessed.ReceiveAsync(ReceiveTimeout);
                await Task.Delay(2000);
                Assert.Equal(2, count);
            }, new QueueMessageProducerSettings(5, TimeSpan.FromSeconds(8), TimeSpan.FromMilliseconds(500)));
        }

        [Fact]
        public Task KeepInvisibleAsync()
        {
            return RunJobQueueTest(async (jobQueueProducer, jobQueueConsumer, queue) =>
            {
                var jobsCompletedStatus = new BufferBlock<JobCompletedStatus>();
                jobQueueConsumer.JobCreated += (job) =>
                {
                    job.Completed += (jobCompleted, ct) =>
                    {
                        return jobsCompletedStatus.SendAsync(jobCompleted.Status, ct);
                    };
                };

                bool first = false;
                jobQueueConsumer.RegisterJobPayloadHandler<JobContentPayload<int>>(
                    async (payload, logger, ct) =>
                    {
                        if (!first)
                        {
                            first = true;
                            await Task.Delay(6000); // 6 secs
                        }
                    }, dataflowBlockOptions: null, options: new JobHandlerOptions() { InvisibleThreshold = TimeSpan.FromSeconds(2) });
                var jobPayload = new JobContentPayload<int>(100);
                await jobQueueProducer.AddJobAsync(jobPayload, null, NullLogger);
                var jobCompletedStatus = await jobsCompletedStatus.ReceiveAsync(ReceiveTimeout);
                Assert.True(jobCompletedStatus.HasFlag(JobCompletedStatus.KeepInvisible));
                await Assert.ThrowsAsync<TimeoutException>(() => jobsCompletedStatus.ReceiveAsync(TimeSpan.FromSeconds(2)));
            }, new QueueMessageProducerSettings(5, TimeSpan.FromSeconds(4), TimeSpan.FromMilliseconds(500)));
        }

        [Fact]
        public async Task AddJobAllAsync()
        {
            var mockControlPlaneInfo = new Mock<IControlPlaneInfo>();
            mockControlPlaneInfo.SetupGet(x => x.AllStamps).Returns(() =>
                new Dictionary<AzureLocation, IControlPlaneStampInfo>()
                {
                    { AzureLocation.WestUs2, null },
                    { AzureLocation.WestEurope, null }
                });
            var jobQueueProducerFactory = new JobQueueProducerFactory(QueueFactory);
            var jobQueueProducerFactoryHelpers = new JobQueueProducerFactoryHelpers(jobQueueProducerFactory, mockControlPlaneInfo.Object);

            var disposables = new List<IAsyncDisposable>();

            var jobQueueConsumerFactory = new JobQueueConsumerFactory(QueueFactory, new NullLogger());

            var queueId = Guid.NewGuid().ToString();

            var payloadsProcessed = new BufferBlock<(JobContentPayload<int>, AzureLocation)>();
            foreach (var location in new AzureLocation[] { AzureLocation.WestUs2, AzureLocation.WestEurope })
            {
                var jobQueueConsumer = (JobQueueConsumer)jobQueueConsumerFactory.GetOrCreate(queueId, location);
                jobQueueConsumer.Start(QueueMessageProducerSettings.Default);
                disposables.Add(jobQueueConsumer);

                jobQueueConsumer.RegisterJobPayloadHandler<JobContentPayload<int>>(
                    async (payload, logger, ct) =>
                    {
                        await payloadsProcessed.SendAsync((payload, location));
                    });
            }

            var jobPayload = new JobContentPayload<int>(100);
            await jobQueueProducerFactoryHelpers.AddJobAllAsync(queueId, jobPayload, null, NullLogger, default);
            disposables.AddRange(jobQueueProducerFactoryHelpers.GetOrCreateAll(queueId).Cast<IAsyncDisposable>());

            var payloadsReceived = new List<(JobContentPayload<int>, AzureLocation)>();

            payloadsReceived.Add(await payloadsProcessed.ReceiveAsync(ReceiveTimeout));
            payloadsReceived.Add(await payloadsProcessed.ReceiveAsync(ReceiveTimeout));
            Assert.Equal(1, payloadsReceived.Count(i => i.Item2 == AzureLocation.WestUs2));
            Assert.Equal(1, payloadsReceived.Count(i => i.Item2 == AzureLocation.WestEurope));

            await Task.WhenAll(disposables.Select(i => i.DisposeAsync().AsTask()));
        }

        [Fact]
        public Task LimitQueueMessagesProducerSettingsAsync()
        {
            return RunJobQueueTest(async (jobQueueProducer, jobQueueConsumer, queue) =>
            {
                int payloadCount = 0;
                var tcs = new TaskCompletionSource<bool>();

                var payloadsProcessed = new BufferBlock<JobContentPayload<int>>();
                jobQueueConsumer.RegisterJobPayloadHandler<JobContentPayload<int>>(
                    async (payload, logger, ct) =>
                    {
                        await payloadsProcessed.SendAsync(payload);
                        if (Interlocked.Increment(ref payloadCount) <= 5)
                        {
                            await tcs.Task;
                        }
                    }, new ExecutionDataflowBlockOptions() { BoundedCapacity = 5, MaxDegreeOfParallelism = 5 } );

                var jobPayload = new JobContentPayload<int>(100);
                for (int i = 0; i < 10; ++i)
                {
                    await jobQueueProducer.AddJobAsync(jobPayload, null, NullLogger);
                }

                // receive the non blocked items
                for (int i = 0; i < 5; ++i)
                {
                    await payloadsProcessed.ReceiveAsync(ReceiveTimeout);
                }

                // next line should timeout
                await Assert.ThrowsAsync<TimeoutException>(() => payloadsProcessed.ReceiveAsync(TimeSpan.FromSeconds(1)));

                // unblock all of the job handlers and so have the producer to queue the 2 last jobs
                tcs.SetResult(true);

                // receive the rest of blocked items
                for (int i = 0; i < 5; ++i)
                {
                    await payloadsProcessed.ReceiveAsync(ReceiveTimeout);
                }
            });
        }

        [Fact]
        public void JobHandlerErrorCallbackAttribute()
        {
            var jobHandler = new MyJobHandler();
            Assert.NotEmpty(jobHandler.GetJobOptions(null).ErrorCallbacks);
            Assert.NotNull(jobHandler.GetJobOptions(null).ErrorCallbacks.FirstOrDefault(i => i is MyJobHandlerErrorCallback));
        }

        [Fact]
        public void JobPayloadAttribute()
        {
            Assert.Equal("JobQueueTest_MyClass_MyPayload", JobPayloadHelpers.GetTypeTag(typeof(MyClass.MyPayload)));
            Assert.Equal("Microsoft_VsSaaS_Services_CloudEnvironments_Jobs_Test_JobQueueTest_MyClass_MyPayload2", JobPayloadHelpers.GetTypeTag(typeof(MyClass.MyPayload2)));
            Assert.Equal("MyCustomPayload3", JobPayloadHelpers.GetTypeTag(typeof(MyClass.MyPayload3)));
        }

        protected Task RunJobQueueTest(Func<JobQueueProducer, JobQueueConsumer, IQueue, Task> testCallback,
            QueueMessageProducerSettings queueMessageProducerSettings = null)
        {
            return RunQueueTest(async (queue) =>
            {
                var jobQueueProducer = new JobQueueProducer(queue);
                var jobQueueConsumer = new JobQueueConsumer(queue, new NullLogger());
                jobQueueConsumer.Start(queueMessageProducerSettings ?? QueueMessageProducerSettings.Default, default);
                await testCallback(jobQueueProducer, jobQueueConsumer, queue);
                await jobQueueProducer.DisposeAsync();
                await jobQueueConsumer.DisposeAsync();
            });
        }

        protected Task RunJobQueueFactoryTest(Func<JobQueueProducerFactory, JobQueueConsumerFactory, IQueue, Task> testCallback)
        {
            return RunQueueTest(async (queue) =>
            {
                var jobQueueProducerFactory = new JobQueueProducerFactory(QueueFactory);
                var jobQueueConsumerFactory = new JobQueueConsumerFactory(QueueFactory, new NullLogger());
                await testCallback(jobQueueProducerFactory, jobQueueConsumerFactory, queue);
            });
        }

        public enum ContinuationState
        {
            None,
            Step1,
            Step2,
            Step3
        }

        private class ContinuationPayload : ContinuationJobPayload<ContinuationState>
        {
            public int InitValue { get; set; }

            public string Step1Value { get; set; }

            public int? Step2Value { get; set; }
        }

        private class ContinuationResult
        {
            public string ResultStep1 { get; set; }

            public int ResultStep2 { get; set; }
        }

        private class ContinuationJobHandler : ContinuationJobHandlerBase<ContinuationPayload, ContinuationState, ContinuationResult>
        {
            public ContinuationJobHandler(IJobQueueProducerFactory jobQueueProducerFactory)
                : base(jobQueueProducerFactory)
            {
            }

            public JobHandlerOptions JobHandlerOptions { set; get; }

            public int Step1Retries { set; get; }

            public override JobHandlerOptions GetJobOptions(IJob<ContinuationPayload> job)
            {
                return JobHandlerOptions ?? base.GetJobOptions(job);
            }

            protected override Task<ContinuationJobResult<ContinuationState, ContinuationResult>> ContinueAsync(IJob<ContinuationPayload> job, IDiagnosticsLogger logger, CancellationToken cancellationToken)
            {
                var payload = job.Payload;
                switch(payload.CurrentState)
                {
                    case ContinuationState.None:
                        break;
                    case ContinuationState.Step1:
                        payload.Step1Value = "hello";
                        break;
                    case ContinuationState.Step2:
                        if (payload.InitValue == 100 || Step1Retries != 0)
                        {
                            if (Step1Retries != 0)
                            {
                                --Step1Retries;
                            }
                            throw new InvalidOperationException();
                        }

                        payload.Step2Value = 100;
                        break;
                    case ContinuationState.Step3:
                        var result = new ContinuationResult() { ResultStep1 = payload.Step1Value, ResultStep2 = payload.Step2Value.Value };
                        return Task.FromResult(ReturnSucceeded(result));
                }

                return Task.FromResult(ReturnNextState(isAutoNextState: true));
            }
        }

        private class MyJobHandlerException : Exception
        {
        }

        public class MyJobHandlerErrorCallback : JobHandlerErrorCallbackBase
        {
            private readonly int maxFailures;
            private int failures;

            public MyJobHandlerErrorCallback(int maxFailures)
            {
                this.maxFailures = maxFailures;
            }

            public MyJobHandlerErrorCallback()
                : this(-1)
            {
            }

            protected override bool DidRetryException(Exception error, out TimeSpan retryTimeout)
            {
                retryTimeout = TimeSpan.FromSeconds(2);
                if (error is MyJobHandlerException)
                {
                    ++this.failures;
                    return (this.maxFailures == -1 || this.failures <= this.maxFailures);
                }

                return false;
            }
        }

        [JobHandlerErrorCallback(typeof(MyJobHandlerErrorCallback))]
        private class MyJobHandler : JobHandlerBase<JobContentPayload<int>>
        {
            public override Task HandleJobAsync(IJob<JobContentPayload<int>> job, IDiagnosticsLogger logger, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private class MyClass
        {
            [JobPayload(JobPayloadNameOption.Name)]
            internal class MyPayload : JobPayload
            {
            }

            [JobPayload(JobPayloadNameOption.FullName)]
            internal class MyPayload2 : JobPayload
            {
            }

            [JobPayload("MyCustomPayload3")]
            internal class MyPayload3 : JobPayload
            {
            }
        }
    }
}
