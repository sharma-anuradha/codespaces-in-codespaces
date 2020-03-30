// <copyright file="ICreateEnvironmentContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Interface for ResumeEnvironmentContinuationHandler.
    /// </summary>
    internal interface ICreateEnvironmentContinuationHandler : IContinuationTaskMessageHandler
    {
    }
}