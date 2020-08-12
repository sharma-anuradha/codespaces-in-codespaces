// <copyright file="ClusteringBasedEnvironmentArchivalTimeCalculator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UsageAnalytics
{
    /// <summary>
    /// An implementation of <see cref="IEnvironmentArchivalTimeCalculator"/> which uses a ML.NET KMeans clustering algorithm
    /// trained with historical billing event data.
    /// </summary>
    public class ClusteringBasedEnvironmentArchivalTimeCalculator : IEnvironmentArchivalTimeCalculator
    {
        private const string LoggerName = "clustering_based_environment_archival_time_calculator";

        /// <summary>
        /// This array contains the best archival value for each of the N clusters.
        /// These clusters have a uint label, going from 1 to N.
        /// This values can be calculated by using /src/Ide/EnvironmentArchiveSimulator/Program.cs
        /// in https://devdiv.visualstudio.com/OnlineServices/_git/vsclk-core?version=GBdev%2Ft-sabar%2Fget-env-data.
        /// Which generates the ModelFile used bellow, and a set of suggested archival values.
        /// </summary>
        private static readonly float[] ArchivalTimesPerCluster = { -1, 1461.7676F, 176.428F, 2592.4268F, 624.7282F, 80.15125F, 101.0303F };
        private static readonly MLContext MlContext = new MLContext(seed: 0);
        private static readonly string ModelFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "kmeans-environment-clustering-model.zip");
        private static readonly double DefaultArchivalHours = 48.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusteringBasedEnvironmentArchivalTimeCalculator"/> class.
        /// </summary
        /// <param name="billingEventRepository">An IBillingEventRepository.</param>
        /// <param name="logger">An IDiagnosticsLogger.</param>
        /// <param name="statsGenerator">An IEnvironmentStatsGenerator.</param>
        public ClusteringBasedEnvironmentArchivalTimeCalculator(
         IBillingEventRepository billingEventRepository,
         IDiagnosticsLogger logger,
         EnvironmentStatsGenerator statsGenerator)
        {
            BillingEventRepository = Requires.NotNull(billingEventRepository, nameof(billingEventRepository));
            Logger = Requires.NotNull(logger, nameof(logger));
            EnvironmentStatsGenerator = Requires.NotNull(statsGenerator, nameof(statsGenerator));
            PredictionEngine = MlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(MlContext.Model.Load(ModelFile, out _));
        }

        private IBillingEventRepository BillingEventRepository { get; }

        private IDiagnosticsLogger Logger { get; }

        private EnvironmentStatsGenerator EnvironmentStatsGenerator { get; }

        private PredictionEngine<ModelInput, ModelOutput> PredictionEngine { get; }

        /// <inheritdoc/>
        public Task<double> ComputeHoursToArchival(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LoggerName}_start_hour_calculation",
                async (innerLogger) =>
                {
                    var duration = logger.StartDuration();
                    var (stats, predictedCluster, hoursToArchive) = await ComputeHoursToArchivalImpl(environment, logger);

                    innerLogger
                    .AddDuration(duration)
                    .FluentAddValue("PredictedCluster", predictedCluster)
                    .FluentAddValue("HoursToArchive", hoursToArchive)
                    .FluentAddValue("TotalTimeActive", stats.TotalTimeActive)
                    .FluentAddValue("TotalTimeSuspend", stats.TotalTimeSuspend)
                    .FluentAddValue("AverageTimeToNextUse", stats.AverageTimeToNextUse)
                    .FluentAddValue("Location", environment.Location)
                    .FluentAddValue("SkuName", environment.SkuName);

                    return hoursToArchive;
                },
                (err, logger) => Task.FromResult(DefaultArchivalHours),
                true);
        }

        private async Task<(EnvironmentStats stats, uint predictedCluster, double hoursToArchive)> ComputeHoursToArchivalImpl(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            var events = await BillingEventRepository.QueryAsync(
                q => q.Where(x => x.Environment.Id == environment.Id && x.Type == BillingEventTypes.EnvironmentStateChange),
                Logger,
                async (_, __) => await Task.CompletedTask);

            var stats = await EnvironmentStatsGenerator.GetStats(events, DateTime.UtcNow, logger.NewChildLogger());

            var clusterPrediction = PredictionEngine.Predict(new ModelInput
            {
                AverageTimeToNextUse = (float)stats.AverageTimeToNextUse,
                AverageTimeToShutdown = (float)stats.AverageTimeToShutdown,
                EnvironmentName = environment.FriendlyName,
                Location = environment.Location.ToString(),
                MaxTimeToNextUse = (float)stats.MaxTimeToNextUse,
                NumTimesActive = (float)stats.NumberOfTimesActive,
                NumTimesShutdown = (float)stats.NumberOfTimeShutdown,
                TimeSpendActive = (float)stats.TotalTimeActive,
                TimeSpentShutdown = (float)stats.TotalTimeSuspend,
            });

            var hoursToArchive = ArchivalTimesPerCluster[clusterPrediction.PredictedLabel];

            return (stats, clusterPrediction.PredictedLabel, hoursToArchive);
        }

        private class ModelInput
        {
            [ColumnName("numTimesActive")]
            [LoadColumn(0)]
            public float NumTimesActive { get; set; }

            [ColumnName("numTimesShutdown")]
            [LoadColumn(1)]
            public float NumTimesShutdown { get; set; }

            [ColumnName("avgTimeToShutdown")]
            [LoadColumn(2)]
            public float AverageTimeToShutdown { get; set; }

            [ColumnName("avgTimeToNextUse")]
            [LoadColumn(3)]
            public float AverageTimeToNextUse { get; set; }

            [ColumnName("maxTimeToNextUse")]
            [LoadColumn(4)]
            public float MaxTimeToNextUse { get; set; }

            [ColumnName("timeSpendActive")]
            [LoadColumn(5)]
            public float TimeSpendActive { get; set; }

            [ColumnName("timeSpentShutdown")]
            [LoadColumn(6)]
            public float TimeSpentShutdown { get; set; }

            [ColumnName("location")]
            [LoadColumn(7)]
            public string Location { get; set; }

            [ColumnName("envName")]
            [LoadColumn(8)]
            public string EnvironmentName { get; set; }
        }

        private class ModelOutput
        {
            public uint PredictedLabel { get; set; }

            public float[] Score { get; set; }

            [ColumnName("envName")]
            public string EnvName { get; set; }
        }
    }
}
