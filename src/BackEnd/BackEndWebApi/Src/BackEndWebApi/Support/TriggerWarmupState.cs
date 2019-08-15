// <copyright file="TriggerWarmupState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApi.Support
{
    /// <summary>
    /// NOTE: This exists due to the fact that HangFire is triggering the creation
    /// of `TriggerWarmup` twice for some reason. I have a question open to them
    /// about whats happening there.
    /// </summary>
    public class TriggerWarmupState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TriggerWarmupState"/> class.
        /// </summary>
        public TriggerWarmupState()
        {
            Status = StatusCodes.Status503ServiceUnavailable;
        }

        /// <summary>
        /// Http status that the endpoint should be returning.
        /// </summary>
        public int Status { get; set; }
    }
}
