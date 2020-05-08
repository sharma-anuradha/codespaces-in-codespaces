// <copyright file="BackplaneManagerConst.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Const for the backplane manager implementation.
    /// </summary>
    public static class BackplaneManagerConst
    {
        /// <summary>
        /// Backplane changes count.
        /// </summary>
        public const string BackplaneChangesCountProperty = "BackplaneChangesCount";

        /// <summary>
        /// Number of secons to push new service metrics.
        /// </summary>
        public const int UpdateMetricsSecs = 45;

        /// <summary>
        /// Number of seconds to consider a service to be 'stale'.
        /// </summary>
        public const int StaleServiceSeconds = UpdateMetricsSecs * 3;
    }
}
