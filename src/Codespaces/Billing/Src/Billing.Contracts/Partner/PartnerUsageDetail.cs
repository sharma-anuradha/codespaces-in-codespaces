// <copyright file="PartnerUsageDetail.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Usage detail.
    /// </summary>
    public class PartnerUsageDetail
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartnerUsageDetail"/> class.
        /// </summary>
        /// <param name="usageDetail">Partner usage detail.</param>
        public PartnerUsageDetail(UsageDetail usageDetail)
        {
            Environments = usageDetail.Environments == null ?
                new PartnerEnvironmentUsageDetail[0] :
                usageDetail
                    .Environments
                    .Select(o => new PartnerEnvironmentUsageDetail(o.Key, o.Value))
                    .Where(o => !o.IsEmpty())
                    .ToArray();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PartnerUsageDetail"/> class.
        /// </summary>
        /// <param name="usageDetail">Partner usage detail.</param>
        public PartnerUsageDetail(IEnumerable<EnvironmentUsage> usageDetail)
        {
            Environments = usageDetail == null ?
                new PartnerEnvironmentUsageDetail[0] :
                usageDetail
                   .Select(o => new PartnerEnvironmentUsageDetail(o))
                    .Where(o => !o.IsEmpty())
                    .ToArray();
        }

        /// <summary>
        /// Gets or sets mapping from environment IDs to environment usage details.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "environments")]
        public PartnerEnvironmentUsageDetail[] Environments { get; set; }
    }
}
