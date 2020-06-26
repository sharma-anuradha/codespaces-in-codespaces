// <copyright file="TestArchiveRunner.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp
{
    /// <summary>
    /// Test Archive Runner.
    /// </summary>
    public class TestArchiveRunner
    {
        private string progressLogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "TestArchive-ProgressOutput.log");

        private object progressLogFileLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestArchiveRunner"/> class.
        /// </summary>
        /// <param name="appSettings">Target app settings.</param>
        /// <param name="environementsRepository">Target environements repository.</param>
        /// <param name="taskHelper">Target task helper.</param>
        public TestArchiveRunner(
            AppSettings appSettings,
            IEnvironementsRepository environementsRepository,
            ITaskHelper taskHelper)
        {
            AppSettings = appSettings;
            EnvironementsRepository = environementsRepository;
            TaskHelper = taskHelper;
        }

        private string LogBaseName { get; } = "test_runner";

        private AppSettings AppSettings { get; }

        private IEnvironementsRepository EnvironementsRepository { get; }

        private ITaskHelper TaskHelper { get; }

        /// <summary>
        /// Runs the actual load test.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<int> RunAsync(IDiagnosticsLogger logger)
        {
            var random = new Random();
            var itemCount = Enumerable.Range(1, 500);
            var repositories = AppSettings.GitRepositories.ToList();
            var region = AppSettings.Regions["WestUs2"];

            // Cleanup any current environment
            await PreCleanup(logger.NewChildLogger());

            await TaskHelper.RunConcurrentEnumerableAsync(
                $"{LogBaseName}_region_run",
                itemCount,
                async (index, childLogger) =>
                {
                    // Staggered start
                    if (index < 5)
                    {
                        await Task.Delay(random.Next(10000, 30000));
                    }

                    // Create environment
                    var timer = Stopwatch.StartNew();
                    ProgressNotification($"{index}: Creating Environment");
                    var environment = await CreateEnvironment(index, repositories, region, childLogger.NewChildLogger());
                    ProgressNotification($"{index}: Creating Environment - {environment.Id} ({timer.Elapsed.TotalSeconds})");

                    childLogger.FluentAddBaseValue("EnvironmentId", environment.Id);

                    // Suspend environment
                    timer.Restart();
                    ProgressNotification($"{index}: Suspend Environment - {environment.Id}");
                    environment = await SuspendEnvironment(environment.Id, childLogger.NewChildLogger());
                    ProgressNotification($"{index}: Suspended Environment - {environment.Id} ({timer.Elapsed.TotalSeconds})");

                    // Wait for archive
                    timer.Restart();
                    ProgressNotification($"{index}: Archive Wait - {environment.Id}");
                    environment = await WaitTillArchived(environment.Id, childLogger.NewChildLogger());
                    ProgressNotification($"{index}: Archive Waited - {environment.Id} ({timer.Elapsed.TotalSeconds})");

                    // Resume environment
                    var deleteEnvironment = true;
                    try
                    {
                        timer.Restart();
                        ProgressNotification($"{index}: Resume Environment - {environment.Id}");
                        environment = await ResumeEnvironment(environment.Id, childLogger.NewChildLogger());
                        ProgressNotification($"{index}: Resumed Environment - {environment.Id} ({timer.Elapsed.TotalSeconds})");
                    }
                    catch (TimeoutException)
                    {
                        // Keep environments that fail to resume
                        deleteEnvironment = false;
                        ProgressNotification($"{index}: Resumed ERROR - {environment.Id}");
                    }

                    // Delete environment
                    if (deleteEnvironment)
                    {
                        timer.Restart();
                        ProgressNotification($"{index}: Delete Environment - {environment.Id}");
                        await DeleteEnvironment(environment.Id, childLogger.NewChildLogger());
                        ProgressNotification($"{index}: Deleted Environment - {environment.Id} ({timer.Elapsed.TotalSeconds})");
                    }
                },
                logger,
                errItemCallback: (index, ex, childLogger) =>
                {
                    ProgressNotification($"{index}: ERROR - {ex.Message} {ex.GetType()}");
                },
                concurrentLimit: 5);

            // Cleanup any current environment
            await PostCleanup(logger.NewChildLogger());

            return 0;
        }

        private void ProgressNotification(string line)
        {
            lock (progressLogFileLock)
            {
                File.AppendAllText(progressLogFilePath, line + Environment.NewLine);
            }
        }

        private Task PreCleanup(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                "environement-pre-cleanup",
                (childLogger) =>
                {
                    // Cleanup file
                    File.Delete(progressLogFilePath);

                    // Same as post cleanup
                    return PostCleanup(childLogger.NewChildLogger());
                });
        }

        private Task PostCleanup(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                "environement-post-cleanup",
                async (childLogger) =>
                {
                    // Fetch list
                    var environments = await EnvironementsRepository.ListEnvironmentsAsync(childLogger.NewChildLogger());

                    // Delete environment
                    foreach (var environment in environments)
                    {
                        await DeleteEnvironment(environment.Id, childLogger.NewChildLogger());
                    }
                });
        }

        private async Task<CloudEnvironmentResult> CreateEnvironment(int i, IList<string> repositories, AppSettingsRegion region, IDiagnosticsLogger logger)
        {
            var repository = repositories[i % repositories.Count];
            var planId = region.AccountPlanId;
            var environment = default(CloudEnvironmentResult);

            // Create environement (caters for pool capacity running out and we want to give it a chance to start filling again.
            await TaskHelper.HttpRetryUntilSuccessOrTimeout(
                "environement-create-item-run",
                async (itemLogger) =>
                {
                    // Create environement
                    environment = await EnvironementsRepository.ProvisionEnvironmentAsync(
                        planId, $"GeneratedName_{Guid.NewGuid()}", repository, null, "standardLinux", itemLogger.NewChildLogger());

                    itemLogger.FluentAddBaseValue("EnvironmentId", environment.Id);
                    logger.FluentAddBaseValue("EnvironmentId", environment.Id);

                    return true;
                },
                TimeSpan.FromMinutes(20),
                TimeSpan.FromSeconds(10),
                logger,
                () => throw new TimeoutException("Pool refresh did not occur in time."));

            // Wait for enivronment to be ready
            await TaskHelper.HttpRetryUntilSuccessOrTimeout(
                "environement-wait-ready-item-run",
                async (itemLogger) =>
                {
                    environment = await EnvironementsRepository.GetEnvironmentAsync(
                        Guid.Parse(environment.Id), itemLogger.NewChildLogger());

                    itemLogger.FluentAddValue("EnvironmentState", environment.State);

                    return environment.State == "Available";
                },
                TimeSpan.FromMinutes(10),
                TimeSpan.FromSeconds(5),
                logger,
                () => throw new TimeoutException("Envirinenent didn't become available in time."));

            return environment;
        }

        private async Task<CloudEnvironmentResult> SuspendEnvironment(string id, IDiagnosticsLogger logger)
        {
            // Trigger suspend
            var environment = await EnvironementsRepository.ShutdownEnvironmentAsync(Guid.Parse(id), logger);

            // Wait for enivronment to be ready
            await TaskHelper.HttpRetryUntilSuccessOrTimeout(
                "environement-resume-item-run",
                async (itemLogger) =>
                {
                    environment = await EnvironementsRepository.GetEnvironmentAsync(
                        Guid.Parse(environment.Id), itemLogger.NewChildLogger());

                    itemLogger.FluentAddValue("EnvironmentState", environment.State);

                    return environment.State == "Shutdown";
                },
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(5),
                logger,
                () => throw new TimeoutException("Envirinenent didn't become shutdown in time."));

            return environment;
        }

        private async Task<CloudEnvironmentResult> WaitTillArchived(string id, IDiagnosticsLogger logger)
        {
            var environment = default(CloudEnvironmentResult);

            // Wait for enivronment to be ready
            await TaskHelper.HttpRetryUntilSuccessOrTimeout(
                "environement-wait-archived-item-run",
                async (itemLogger) =>
                {
                    environment = await EnvironementsRepository.GetEnvironmentAsync(
                        Guid.Parse(id), itemLogger.NewChildLogger());

                    itemLogger.FluentAddValue("EnvironmentLastStateUpdateReason", environment.LastStateUpdateReason);

                    return environment.LastStateUpdateReason == "EnvironmentArchived";
                },
                TimeSpan.FromMinutes(30),
                TimeSpan.FromSeconds(10),
                logger,
                () => throw new TimeoutException("Envirinenent didn't Archive in time."));

            return environment;
        }

        private async Task<CloudEnvironmentResult> ResumeEnvironment(string id, IDiagnosticsLogger logger)
        {
            // Trigger suspend
            var environment = await EnvironementsRepository.ResumeEnvironmentAsync(Guid.Parse(id), logger);

            // Wait for enivronment to be ready
            await TaskHelper.HttpRetryUntilSuccessOrTimeout(
                "environement-resume-item-run",
                async (itemLogger) =>
                {
                    environment = await EnvironementsRepository.GetEnvironmentAsync(
                        Guid.Parse(environment.Id), itemLogger.NewChildLogger());

                    itemLogger.FluentAddValue("EnvironmentState", environment.State);

                    return environment.State == "Available";
                },
                TimeSpan.FromMinutes(10),
                TimeSpan.FromSeconds(5),
                logger,
                () => throw new TimeoutException("Envirinenent didn't Shutdown in time."));

            return environment;
        }

        private async Task DeleteEnvironment(string id, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
                "environement-delete-item-run",
                async (childLogger) =>
                {
                    childLogger.FluentAddValue("EnvironmentId", id);

                    await EnvironementsRepository.DeleteEnvironmentAsync(
                        Guid.Parse(id), childLogger.NewChildLogger());
                });
        }
    }
}
