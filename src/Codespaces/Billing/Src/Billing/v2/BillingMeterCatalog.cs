// <copyright file="BillingMeterCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <inheritdoc/>
    public class BillingMeterCatalog : IBillingMeterCatalog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BillingMeterCatalog"/> class.
        /// </summary>
        /// <param name="billingMeterSettings">Billing Meter Settings.</param>
        public BillingMeterCatalog(
            IOptions<BillingMeterSettings> billingMeterSettings)
            : this(billingMeterSettings.Value.MetersByLocation, billingMeterSettings.Value.MetersByResource)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingMeterCatalog"/> class.
        /// </summary>
        /// <param name="metersByLocation">ReadOnly dictionary of meters by location.</param>
        /// <param name="metersByResource">ReadOnly dictionary of meters by resource type.</param>
        public BillingMeterCatalog(IDictionary<AzureLocation, string> metersByLocation, ResourceBillingMeters metersByResource)
        {
            MetersByLocation = new ReadOnlyDictionary<AzureLocation, string>(metersByLocation);

            MetersByResource = metersByResource;
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<AzureLocation, string> MetersByLocation { get; } = new Dictionary<AzureLocation, string>();

        /// <inheritdoc/>
        public ResourceBillingMeters MetersByResource { get; } = new ResourceBillingMeters();
    }
}
