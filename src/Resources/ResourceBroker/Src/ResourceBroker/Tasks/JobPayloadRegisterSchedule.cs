// <copyright file="JobPayloadRegisterSchedule.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Job payload register schedule with Cron expression and lease interval
    /// </summary>
    public static class JobPayloadRegisterSchedule
    {
        // BaseResourceImage jobs run every day at 00:00 ("0 0 * * *") with 1 day lease
        public static readonly (string CronExpression, TimeSpan Interval) BaseResourceImageJobSchedule = ("0 0 * * *", TimeSpan.FromDays(1));

        // DeleteResourceGroupDeployments jobs run every hour ("0 * * * *") with 1 hour lease
        public static readonly (string CronExpression, TimeSpan Interval) DeleteResourceGroupDeploymentsJobSchedule = ("0 * * * *", TimeSpan.FromHours(1));

        // LogSystemResourceState jobs run every 10 minutes ("*/10 * * * *") with 10 minutes lease 
        public static readonly (string CronExpression, TimeSpan Interval) LogSystemResourceStateJobSchedule = ("*/10 * * * *", TimeSpan.FromMinutes(10));

        // WatchOrphanedAzureResource jobs run every 30 minutes ("*/30 * * * *") with 30 minutes lease
        public static readonly (string CronExpression, TimeSpan Interval) WatchOrphanedAzureResourceJobSchedule = ("*/30 * * * *", TimeSpan.FromMinutes(30));

        // WatchOrphanedPool jobs run every day at 00:00 ("0 0 * * *") with 1 day lease
        public static readonly (string CronExpression, TimeSpan Interval) WatchOrphanedPoolJobSchedule = ("0 0 * * *", TimeSpan.FromDays(1));

        // WatchOrphanedSystemResource jobs run every 2 hours ("0 0/2 * * *") with 20 minutes lease
        public static readonly (string CronExpression, TimeSpan Interval) WatchOrphanedSystemResourceJobSchedule = ("0 0/2 * * *", TimeSpan.FromMinutes(20));

        // WatchPool jobs run every minute ("* * * * *") with 1 minute lease
        public static readonly (string CronExpression, TimeSpan Interval) WatchPoolJobSchedule = ("* * * * *", TimeSpan.FromMinutes(1));

        // WatchOrphanedComputeImages jobs run every day at 00:00 ("0 0 * * *") with 1 day lease
        public static readonly (string CronExpression, TimeSpan Interval) WatchOrphanedComputeImagesJobSchedule = ("0 0 * * *", TimeSpan.FromDays(1));
    }
}
