// <copyright file="JobCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common
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
    }
}
