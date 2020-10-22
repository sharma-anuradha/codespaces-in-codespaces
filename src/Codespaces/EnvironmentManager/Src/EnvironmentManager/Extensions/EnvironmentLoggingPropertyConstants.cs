// <copyright file="EnvironmentLoggingPropertyConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions
{
    /// <summary>
    /// Logging constants for resource broker.
    /// </summary>
    public class EnvironmentLoggingPropertyConstants
    {
        /// <summary>
        /// Environment Id.
        /// </summary>
        public const string EnvironmentId = nameof(EnvironmentId);

        /// <summary>
        /// Operation Reason.
        /// </summary>
        public const string OperationReason = nameof(OperationReason);

        // <summary>
        /// Operation Reason.
        /// </summary>
        public const string PoolLocation = nameof(PoolLocation);

        /// <summary>
        /// Pool Sku Name.
        /// </summary>
        public const string PoolSkuName = nameof(PoolSkuName);

        /// <summary>
        /// Pool Resource Type.
        /// </summary>
        public const string PoolResourceType = nameof(PoolResourceType);

        /// <summary>
        /// Pool Definition.
        /// </summary>
        public const string PoolDefinition = nameof(PoolDefinition);

        /// <summary>
        /// Pool Target Count name.
        /// </summary>
        public const string PoolTargetCount = nameof(PoolTargetCount);

        /// <summary>
        /// Pool Override Target Count name.
        /// </summary>
        public const string PoolOverrideTargetCount = nameof(PoolOverrideTargetCount);

        /// <summary>
        /// Pool Is Enabled name.
        /// </summary>
        public const string PoolIsEnabled = nameof(PoolIsEnabled);

        /// <summary>
        /// Pool Override Is Enabled name.
        /// </summary>
        public const string PoolOverrideIsEnabled = nameof(PoolOverrideIsEnabled);

        /// <summary>
        /// Max Create Batch Count name.
        /// </summary>
        public const string MaxCreateBatchCount = nameof(MaxCreateBatchCount);

        /// <summary>
        /// Max Delete Batch Count name.
        /// </summary>
        public const string MaxDeleteBatchCount = nameof(MaxDeleteBatchCount);
    }
}
