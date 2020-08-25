// <copyright file="GitHubWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    ///  Background worker that's used to transfer billing summaries to GitHub.
    /// </summary>
    public class GitHubWorker : PartnerWorker
    {
        private const double Interval = 60;
        private const string Name = nameof(Partner.GitHub);

        /// <summary>
        /// Initializes a new instance of the <see cref="GitHubWorker"/> class.
        /// </summary>
        /// <param name="partnerService">the partner service that runs the actual operation.</param>
        /// <param name="diagnosticsLogger">the logger.</param>
        public GitHubWorker(
            BillingSettings billingSettings,
            IPartnerService partnerService,
            IDiagnosticsLogger diagnosticsLogger)
            : base(billingSettings, partnerService, diagnosticsLogger, Name, Interval)
        {
        }
    }
}