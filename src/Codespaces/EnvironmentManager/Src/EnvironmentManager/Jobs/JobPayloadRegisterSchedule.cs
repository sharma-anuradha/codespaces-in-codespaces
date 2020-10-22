// <copyright file="JobPayloadRegisterSchedule.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Jobs
{
    /// <summary>
    /// Job payload register schedule with Cron expression and lease interval
    /// </summary>
    public static class JobPayloadRegisterSchedule
    {
        // WatchPool jobs run every minute ("* * * * *") with 1 minute lease
        public static readonly (string CronExpression, TimeSpan Interval) WatchPoolJobSchedule = ("* * * * *", TimeSpan.FromMinutes(1));
    }
}
