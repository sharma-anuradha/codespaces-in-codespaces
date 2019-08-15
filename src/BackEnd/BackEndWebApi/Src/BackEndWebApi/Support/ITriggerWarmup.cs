// <copyright file="ITriggerWarmup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApi.Support
{
    /// <summary>
    /// Triggers tasks that need to run on warmup. 
    /// </summary>
    public interface ITriggerWarmup
    {
        /// <summary>
        /// Triggers the warmup execution to start.
        /// </summary>
        /// <returns>Http status code to be returned.</returns>
        int Start();
    }
}
