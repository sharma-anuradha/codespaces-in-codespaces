// <copyright file="JobCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Job command.
    /// </summary>
    public enum JobCommand
    {
        /// <summary>
        /// Unknown job.
        /// </summary>
        NotImplemented = 0,

        /// <summary>
        /// Start environment job.
        /// </summary>
        StartEnvironment = 1,

        /// <summary>
        /// Export environment job.
        /// </summary>
        ExportEnvironment = 2,
    }
}
