// <copyright file="IInactiveEnvironmentWorkflow.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows
{
    /// <summary>
    /// An interface to support componsition for the InactiveEnvironmentWorkflow.
    /// </summary>
    public interface IInactiveEnvironmentWorkflow : IEnvironmentRepairWorkflow
    {
    }
}
