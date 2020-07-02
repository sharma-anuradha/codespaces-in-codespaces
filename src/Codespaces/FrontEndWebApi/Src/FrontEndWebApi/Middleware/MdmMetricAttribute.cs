// <copyright file="MdmMetricAttribute.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Cloud.InstrumentationFramework.Metrics.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.AspNetCore.Diagnostics.Middleware
{
    /// <summary>
    /// Metric Attribute used to decorate controller's actions that should be logged.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class MdmMetricAttribute : MdmMetricAttributeBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MdmMetricAttribute"/> class.
        /// </summary>
        /// <param name="name"> Metric name.</param>
        /// <param name="metricNamespace"> Metric name space.</param>
        public MdmMetricAttribute(string name, string metricNamespace)
            : base(name, metricNamespace)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MdmMetricAttribute"/> class.
        /// </summary>
        /// <param name="name"> Metric name.</param>
        /// <param name="metricNamespace"> Metric name space.</param>
        /// <param name="latencyBehavior"> LatencyBehavior for bucket distribution behavior.</param>
        public MdmMetricAttribute(string name, string metricNamespace, MdmBucketedDistributionBehavior latencyBehavior)
            : base(name, metricNamespace, latencyBehavior)
        {
        }

        private string AccountName { get; set; }

        private bool IsEnabledForSvc { get; set; }

        private string PlanId { get; set; }

        /// <inheritdoc/>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var mdmMetricSettings = ApplicationServicesProvider.GetRequiredService<MdmMetricSettings>();
            PlanId = context.HttpContext.RequestServices.GetService<ICurrentUserProvider>()?.Identity?.AuthorizedPlan;
            IsEnabledForSvc = mdmMetricSettings.Enable;
            AccountName = mdmMetricSettings.AccountName;
        }

        /// <inheritdoc/>
        protected override string GetAccountName()
        {
            return AccountName;
        }

        /// <inheritdoc/>
        protected override string GetCustomerResourceId()
        {
            return PlanId;
        }

        /// <inheritdoc/>
        protected override bool IsMdmMetricEnabledForSvc()
        {
            return IsEnabledForSvc;
        }
    }
}