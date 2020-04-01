// <copyright file="EnvironmentDeletionContinuationInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PCFAgent
{
    /// <summary>
    /// Environment deletion input for continuation framework.
    /// </summary>
    public class EnvironmentDeletionContinuationInput : ContinuationInput
    {
        /// <summary>
        /// Gets or sets the environment Id.
        /// </summary>
        public string EnvironmentId { get; set; }
    }
}
