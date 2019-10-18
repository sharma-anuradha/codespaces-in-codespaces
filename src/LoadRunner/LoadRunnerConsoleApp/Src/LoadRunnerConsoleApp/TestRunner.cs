// <copyright file="TestRunner.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp
{
    /// <summary>
    /// Core test runner.
    /// </summary>
    public class TestRunner
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunner"/> class.
        /// </summary>
        /// <param name="appSettings">Target app settings.</param>
        /// <param name="environementsRepository">Target environements repository.</param>
        /// <param name="resourcePoolSettingsRepository">Target resource pool settings repository.</param>
        /// <param name="resourcePoolStateSnapshotRepository">Target resource pool state snapshot repository.</param>
        /// <param name="taskHelper">Target task helper.</param>
        public TestRunner(
            AppSettings appSettings,
            IEnvironementsRepository environementsRepository,
            IResourcePoolSettingsRepository resourcePoolSettingsRepository,
            IResourcePoolStateSnapshotRepository resourcePoolStateSnapshotRepository,
            ITaskHelper taskHelper)
        {
            AppSettings = appSettings;
            EnvironementsRepository = environementsRepository;
            ResourcePoolSettingsRepository = resourcePoolSettingsRepository;
            ResourcePoolStateSnapshotRepository = resourcePoolStateSnapshotRepository;
            TaskHelper = taskHelper;
        }

        private string LogBaseName { get; } = "test_runner";

        private AppSettings AppSettings { get; }

        private IEnvironementsRepository EnvironementsRepository { get; }

        private IResourcePoolSettingsRepository ResourcePoolSettingsRepository { get; }

        private IResourcePoolStateSnapshotRepository ResourcePoolStateSnapshotRepository { get; }

        private ITaskHelper TaskHelper { get; }

        /// <summary>
        /// Runs the actual load test.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public Task<int> RunAsync(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                LogBaseName,
                async (childLogger) =>
                {
                    // Pre cleanup
                    var poolCodes = await PreExecutionCleanupAsync(childLogger);

                    // Pre execution setup
                    await PreExecutionSetupAsync(poolCodes, childLogger);

                    // Run tests
                    await RunCoreTestAsync(childLogger);

                    // Post cleanup
                    await PostExecutionCleanupAsync(poolCodes, childLogger);

                    return 0;
                });
        }

        private async Task<IEnumerable<string>> PreExecutionCleanupAsync(IDiagnosticsLogger logger)
        {
            // Fetch current snapshot
            var poolSnapshots = await FetchPoolSnapshotsAsync(logger.NewChildLogger());

            // Delete all snapshots (so that we clear out any old pools)
            await poolSnapshots.ForEachAsync(
                x => ResourcePoolStateSnapshotRepository.DeleteAsync(x.Id, logger.NewChildLogger()));

            var poolCodes = default(IEnumerable<string>);

            // Wait for snapshots to come back in
            await TaskHelper.RetryUntilSuccessOrTimeout(
                "pre_execution_cleanup_pool_spin_up",
                async (childLogger) =>
                {
                    // Fetch current snapshot
                    poolCodes = (await FetchPoolSnapshotsAsync(logger)).Select(x => x.Id);

                    // Avoid possible race considition where pool was still populating
                    if (poolCodes.Any())
                    {
                        await Task.Delay(10000);
                        poolCodes = (await FetchPoolSnapshotsAsync(logger)).Select(x => x.Id);
                    }

                    // Check that all pools are empty
                    return poolCodes.Any();
                },
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(10),
                logger,
                () => throw new Exception("Pool snapshots didn't appear within alloted time."));

            // Currently the same
            await PostExecutionCleanupAsync(poolCodes, logger);

            return poolCodes;
        }

        private async Task PreExecutionSetupAsync(IEnumerable<string> poolCodes, IDiagnosticsLogger logger)
        {
            // Enable pools
            await poolCodes.ForEachAsync(
                x => ResourcePoolSettingsRepository.CreateOrUpdateAsync(
                    new ResourcePoolSettingsRecord { Id = x, IsEnabled = true }, logger.NewChildLogger()));

            // Check that pool levels are at 0, wait till 0 if not
            await TaskHelper.RetryUntilSuccessOrTimeout(
                "pre_execution_cleanup_pool_spin_up",
                async (childLogger) =>
                {
                    // Fetch current snapshot
                    var poolSnapshots = await FetchPoolSnapshotsAsync(childLogger);
                    var currentSnapshots = poolSnapshots.Where(x => x.IsEnabled);

                    // Check that all pools are empty
                    return currentSnapshots.Any() && currentSnapshots
                        .Select(x => x.ReadyUnassignedCount >= (x.TargetCount * 0.5))
                        .Aggregate((result, x) => result && x);
                },
                TimeSpan.FromMinutes(35), // Takes a while for all the storage accounts to come up
                TimeSpan.FromSeconds(20),
                logger,
                () => throw new Exception("Pool populate didn't occur within alloted time."));
        }

        private async Task RunCoreTestAsync(IDiagnosticsLogger logger)
        {
            var regions = AppSettings.Regions.Where(x => x.Value.Enabled);
            var repositories = AppSettings.GitRepositories.ToList();
            var regionEnvironments = new Dictionary<string, IList<string>>();

            // Iterate through each region we have, working on 3 at a time
            await TaskHelper.RunBackgroundEnumerableAsync(
                "region-environement-create-run",
                regions,
                async (region, childLogger) =>
                {
                    var environments = new List<string>();
                    var accountId = (string)null; // region.Value.AuthAccountId;
                    var location = region.Key;

                    // Request total environments needed
                    for (var i = 0; i < region.Value.TotalEnvironementRunCount; i++)
                    {
                        // Setup environment inputs
                        var repository = repositories[i % repositories.Count];

                        // Try provisioning the environement till we get one, this is
                        // designed to cater for the case where the pool capacity runs
                        // out and we want to give it a chance to start filling again.
                        await TaskHelper.RetryUntilSuccessOrTimeout(
                            "region-environement-create-item-run",
                            async (itemLogger) =>
                            {
                                try
                                {
                                    // Create environement
                                    var result = await EnvironementsRepository.ProvisionEnvironmentAsync(
                                        accountId, $"GeneratedName_{Guid.NewGuid().ToString()}", repository, location, null, itemLogger.NewChildLogger());

                                    // Record the environment
                                    environments.Add(result.Id);

                                    return true;
                                }
                                catch (HttpResponseStatusException e) when (e.StatusCode == HttpStatusCode.ServiceUnavailable)
                                {
                                    var delay = e.RetryAfter.HasValue ? e.RetryAfter.Value * 1000 : 30000;
                                    await Task.Delay(delay);
                                }
                                catch (Exception e)
                                {
                                    throw;
                                }

                                return false;
                            },
                            TimeSpan.FromMinutes(20),
                            null,
                            childLogger,
                            () => throw new Exception("Pool refresh did not occur in time."));

                        // Drain these out slowly
                        await Task.Delay(1000);
                    }

                    regionEnvironments.Add(region.Key, environments);
                },
                logger);

            // Now that everything is running, lets start deallocating
            await TaskHelper.RunBackgroundEnumerableAsync(
                "region-environement-deallocate-run",
                regionEnvironments,
                async (region, childLogger) =>
                {
                    var environments = region.Value;
                    var location = region.Key;

                    // Request total environments needed
                    foreach (var environement in environments)
                    {
                        // Delete the environement
                        await EnvironementsRepository.DeleteEnvironmentAsync(
                            Guid.Parse(environement), logger.NewChildLogger());

                        // Drain these out slowly
                        await Task.Delay(200);
                    }
                },
                logger);

            // When service starts returning pool empty, slow requests to once evey 5 seconds
            // ...
            // Once this phase is started, put a 30min timeout (i.e. if it keeps getting consecutive
            // pool empty for 30 minutes, abort test

            // Continue requesting environemetns till 250 environements have been created
            // ...
        }

        private async Task PostExecutionCleanupAsync(IEnumerable<string> poolCodes, IDiagnosticsLogger logger)
        {
            // Make sure that pools are disabled
            await poolCodes.ForEachAsync(
                x => ResourcePoolSettingsRepository.CreateOrUpdateAsync(
                    new ResourcePoolSettingsRecord { Id = x, IsEnabled = false }, logger.NewChildLogger()));

            // Fetch any existing environments
            var existingEnvironments = await EnvironementsRepository.ListEnvironmentsAsync(logger.NewChildLogger());

            // Delete existing environemnts
            await existingEnvironments.ForEachAsync(
                x => EnvironementsRepository.DeleteEnvironmentAsync(
                        Guid.Parse(x.Id), logger.NewChildLogger()),
                TimeSpan.FromMilliseconds(100));

            // Fetch any existing environments and make sure we don't have  any after delete
            existingEnvironments = await EnvironementsRepository.ListEnvironmentsAsync(logger.NewChildLogger());
            if (existingEnvironments.Any())
            {
                throw new Exception("User environement cleanup failed.");
            }

            // Check that pool levels are at 0, wait till 0 if not
            await TaskHelper.RetryUntilSuccessOrTimeout(
                "post_execution_cleanup_pool_wind_down",
                async (childLogger) =>
                {
                    // Fetch current snapshot
                    var poolSnapshots = await FetchPoolSnapshotsAsync(childLogger);

                    // Check that all pools are empty
                    return poolSnapshots
                        .Select(x => x.UnassignedCount)
                        .Aggregate((result, x) => result + x) == 0;
                },
                TimeSpan.FromMinutes(5),  // Should be fairly quick, as soon as delete is triggered, will not show is pool
                TimeSpan.FromSeconds(20),
                logger,
                () => throw new Exception("Pool drain didn't occur within alloted time."));
        }

        private async Task<IEnumerable<ResourcePoolStateSnapshotRecord>> FetchPoolSnapshotsAsync(IDiagnosticsLogger logger)
        {
            // Fetch data so that we can get pool codes
            return await ResourcePoolStateSnapshotRepository.GetWhereAsync(x => true, logger.NewChildLogger());
        }
    }
}
