// <copyright file="JobOperationResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common
{
    /// <summary>
    /// Store result for tasks in job.
    /// </summary>
    public class JobOperationResult
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether task was successfull.
        /// </summary>
        public bool Succeeded { get; set; }
    }

}
