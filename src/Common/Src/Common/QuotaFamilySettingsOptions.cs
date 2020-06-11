// <copyright file="QuotaFamilySettingsOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// The quota family options.
    /// </summarys
    public class QuotaFamilySettingsOptions
    {
        /// <summary>
        /// Gets or sets the catalog settings.
        /// </summary>
        public IDictionary<string, IDictionary<string, int>> Settings { get; set; } = new Dictionary<string, IDictionary<string, int>>();
    }
}
