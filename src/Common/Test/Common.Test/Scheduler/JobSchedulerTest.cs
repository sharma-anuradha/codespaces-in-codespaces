using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler.Contracts;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Test.Scheduler
{
    public class JobSchedulerTest
    {
        private readonly static TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(60);

        [Fact]
        public async Task Recurring5SecSimpleAsync()
        {
            var runJobs = new BufferBlock<DateTime>();
            await RunTest(
                async (jobScheduler) =>
                {
                    for (int i = 0; i < 2; ++i)
                    {
                        var scheduleRun = await runJobs.ReceiveAsync(ReceiveTimeout);
                        Assert.Equal(0, scheduleRun.Second % 5);
                    }
                },
                (jobScheduler) =>
                {
                    // define a 5 secs before starting the job scheduler
                    jobScheduler.AddRecurringJob("0/5 * * * * *", "test_5secs", async (jobRunId, dt, srvc, logger, ct) =>
                    {
                        await runJobs.SendAsync(dt);
                    });
                });
        }

        [Fact]
        public async Task AddWhenStartedAsync()
        {
            await RunTest(
                async (jobScheduler) =>
                {
                    var runJobs = new BufferBlock<DateTime>();
                    // define a 1 sec 
                    jobScheduler.AddDelayedJob(TimeSpan.FromSeconds(1), "delayed 1 sec", (jobRunId, dt, srvc, logger, ct) =>
                    {
                        return runJobs.SendAsync(dt);
                    });

                    await runJobs.ReceiveAsync(ReceiveTimeout);
                },
                (jobScheduler) =>
                {
                    // define a long recurring job before starting the job scheduler
                    jobScheduler.AddRecurringJob("0 12 * * SUN", "noon on sunday", (jobRunId, dt, srvc, logger, ct) =>
                    {
                        return Task.CompletedTask;
                    });
                });
        }

        [Fact]
        public async Task RunNow()
        {
            await RunTest(
                async (jobScheduler) =>
                {
                    var runJobs = new BufferBlock<DateTime>();
                    // define a 1 sec 
                    var delayedJob = jobScheduler.AddDelayedJob(TimeSpan.FromDays(1), "delayed 1 day", (jobRunId, dt, srvc, logger, ct) =>
                    {
                        return runJobs.SendAsync(dt);
                    });

                    await delayedJob.RunNowAsync(default);
                    await runJobs.ReceiveAsync(ReceiveTimeout);
                });
        }

        [Fact]
        public async Task GetScheduleJobs()
        {
            await RunTest(
                (jobScheduler) =>
                {
                    jobScheduler.AddDelayedJob(TimeSpan.FromDays(1), "job#1", (jobRunId, dt, srvc, logger, ct) =>
                    {
                        return Task.CompletedTask;
                    });
                    jobScheduler.AddDelayedJob(TimeSpan.FromDays(1), "job_type_1", (jobRunId, dt, srvc, logger, ct) =>
                    {
                        return Task.CompletedTask;
                    });
                    jobScheduler.AddDelayedJob(TimeSpan.FromDays(1), "job_type_1", (jobRunId, dt, srvc, logger, ct) =>
                    {
                        return Task.CompletedTask;
                    });

                    Assert.Single(jobScheduler.GetScheduleJobs("job#1"));
                    Assert.Equal(2, jobScheduler.GetScheduleJobs("job_type_1").Count());
                    return Task.CompletedTask;
                });
        }

        [Fact]
        public async Task DisposeAsync()
        {
            await RunTest(
                async (jobScheduler) =>
                {
                    var runJobs = new BufferBlock<DateTime>();
                    // define a 1 sec 
                    var nextSecJob = jobScheduler.AddDelayedJob(TimeSpan.FromSeconds(1), "delayed 1 sec", (jobRunId, dt, srvc, logger, ct) =>
                    {
                        return runJobs.SendAsync(dt);
                    });
                    await Task.Delay(100);
                    nextSecJob.Dispose();
                    await Task.Delay(2000);
                    Assert.Equal(0, runJobs.Count);
                });
        }

        [Fact]
        public async Task CancelJobAsync()
        {
            await RunTest(
                async (jobScheduler) =>
                {
                    // define a 1 sec 
                    var nextSecJob = jobScheduler.AddDelayedJob(TimeSpan.FromSeconds(1), "delayed 1 sec", (jobRunId, dt, srvc, logger, ct) =>
                    {
                        return Task.Delay(5000, ct);
                    });
                    nextSecJob.JobStart += (jobStartInfo) =>
                    {
                        jobStartInfo.Cancel();
                        return Task.CompletedTask;
                    };
                    var endJobs = new BufferBlock<IJobEndInfo>();
                    nextSecJob.JobEnd += (jobEndInfo) =>
                    {
                        return endJobs.SendAsync(jobEndInfo);
                    };

                    var jobEndInfo = await endJobs.ReceiveAsync(ReceiveTimeout);
                    Assert.IsType<TaskCanceledException>(jobEndInfo.Exception);
                });
        }

        [Fact]
        public async Task RunJobExceptionAsync()
        {
            await RunTest(
                async (jobScheduler) =>
                {
                    // define a 1 sec 
                    var nextSecJob = jobScheduler.AddDelayedJob(TimeSpan.FromSeconds(1), "delayed 1 sec", (jobRunId, dt, srvc, logger, ct) =>
                    {
                        throw new ArgumentException();
                    });
                    var endJobs = new BufferBlock<IJobEndInfo>();
                    nextSecJob.JobEnd += (jobEndInfo) =>
                    {
                        return endJobs.SendAsync(jobEndInfo);
                    };

                    var jobEndInfo = await endJobs.ReceiveAsync(ReceiveTimeout);
                    Assert.IsType<ArgumentException>(jobEndInfo.Exception);
                });
        }

        private static async Task RunTest(
            Func<JobScheduler, Task> test,
            Action<JobScheduler> init = null)
        {
            var jobScheduler = new JobScheduler(CreateServiceScopeFactory(), new NullLogger());

            init?.Invoke(jobScheduler);
            await jobScheduler.StartAsync(default);

            await test(jobScheduler);
            await jobScheduler.StopAsync(default);
        }

        private static IServiceScopeFactory CreateServiceScopeFactory()
        {
            var mockServiceScope = new Mock<IServiceScope>();
            mockServiceScope.Setup(e => e.Dispose()).Callback(() =>
            {
            });
            mockServiceScope.SetupGet(x => x.ServiceProvider).Returns(() => null);

            var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            mockServiceScopeFactory.Setup(e => e.CreateScope()).Returns(() => mockServiceScope.Object);

            return mockServiceScopeFactory.Object;
        }
    }
}
