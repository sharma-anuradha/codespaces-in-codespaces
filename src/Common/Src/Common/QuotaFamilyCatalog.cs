// <copyright file="QuotaFamilyCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Stores information about quotas per family.
    /// </summary>
    public class QuotaFamilyCatalog : IQuotaFamilyCatalog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuotaFamilyCatalog"/> class.
        /// </summary>
        /// <param name="quotaFamilySettingsOptions">The options instance for the quota family catalog.</param>
        /// <param name="logger">The diagnostics logger.</param>
        public QuotaFamilyCatalog(
            IOptions<QuotaFamilySettingsOptions> quotaFamilySettingsOptions)
            : this(quotaFamilySettingsOptions.Value.Settings)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuotaFamilyCatalog"/> class.
        /// </summary>
        /// <param name="quotaFamilyCatalog">the Quota family.</param>
        public QuotaFamilyCatalog(IDictionary<string, IDictionary<string, int>> quotaFamilyCatalog)
        {
            QuotaFamilies = new ReadOnlyDictionary<string, IDictionary<string, int>>(quotaFamilyCatalog);
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, IDictionary<string, int>> QuotaFamilies { get; } = new Dictionary<string, IDictionary<string, int>>();
    }
}
