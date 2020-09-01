// <copyright file="GitHubService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// The service that pushes billing summeries into partner queues.
    /// </summary>
    public class GitHubService : PartnerService, IPartnerService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubService"/> class.
        /// </summary>
        /// <param name="controlPlanInfo">Needed to find all available control plans.</param>
        /// <param name="billingEventManager">used to get billing summaries and plan.</param>
        /// <param name="logger">the logger.</param>
        /// <param name="gitHubStorageFactory">used to get billing storage collections.</param>
        /// <param name="claimedDistributedLease"> the lease holder.</param>
        /// <param name="taskHelper">the task helper.</param>
        /// <param name="planManager">Used to get the list of plans to bill.</param>
        public GitHubService(
            IControlPlaneInfo controlPlanInfo,
            IBillingEventManager billingEventManager,
            IDiagnosticsLogger logger,
            IPartnerCloudStorageFactory gitHubStorageFactory,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper,
            IPlanManager planManager)

            // linting rules will now allow breaking this up across multipule lines
            : base(controlPlanInfo, billingEventManager, logger, gitHubStorageFactory, claimedDistributedLease, taskHelper, planManager)
        {
        }

        /// <inheritdoc/>
        protected override string ServiceName => "github-billingsub-worker";

        /// <inheritdoc/>
        protected override string PartnerId => "gh";

        /// <inheritdoc/>
        protected override Partner Partner => Partner.GitHub;
    }
}
