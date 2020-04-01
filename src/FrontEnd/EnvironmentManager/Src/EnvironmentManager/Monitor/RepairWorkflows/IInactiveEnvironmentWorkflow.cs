// <copyright file="IInactiveEnvironmentWorkflow.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Monitor.RepairWorkflows
{
    /// <summary>
    /// An interface to support componsition for the InactiveEnvironmentWorkflow.
    /// </summary>
    internal interface IInactiveEnvironmentWorkflow : IEnvironmentRepairWorkflow
    {
    }
}
