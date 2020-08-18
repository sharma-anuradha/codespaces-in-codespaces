// <copyright file="PlanNotFoundException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
    /// <summary>
    /// A billing plan was not found.
    /// </summary>
    public class PlanNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlanNotFoundException"/> class.
        /// </summary>
        /// <param name="planInfo">The plan info.</param>
        /// <param name="inner">The inner exception.</param>
        public PlanNotFoundException(
            VsoPlanInfo planInfo,
            Exception inner = null)
            : base($"The plan resource '{planInfo?.ResourceId}' was not found.", inner)
        {
            PlanInfo = planInfo;
        }

        /// <summary>
        /// Gets the resource id.
        /// </summary>
        public VsoPlanInfo PlanInfo { get; }
    }
}
