// <copyright file="PrepareFileShareTaskInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// Task and job information of the current task that's preparing the file share.
    /// </summary>
    public class PrepareFileShareTaskInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrepareFileShareTaskInfo"/> class.
        /// </summary>
        public PrepareFileShareTaskInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrepareFileShareTaskInfo"/> class.
        /// </summary>
        /// <param name="jobId"><see cref="JobId"/>.</param>
        /// <param name="taskId"><see cref="TaskId"/>.</param>
        /// <param name="taskLocation"><see cref="TaskLocation"/>.</param>
        public PrepareFileShareTaskInfo(
            string jobId,
            string taskId,
            string taskLocation)
        {
            JobId = jobId;
            TaskId = taskId;
            TaskLocation = taskLocation;
        }

        /// <summary>
        /// Gets or sets the job id.
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Gets or sets the task id.
        /// </summary>
        public string TaskId { get; set; }

        /// <summary>
        /// Gets or sets the azure location the batch task is in.
        /// </summary>
        public string TaskLocation { get; set; }
    }
}