﻿// <copyright file="ComputeOS.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Specifies the Cloud Enivronment OS type.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ComputeOS
    {
        /// <summary>
        /// Linux OS
        /// </summary>
        Linux = 1,

        /// <summary>
        /// Windows OS
        /// </summary>
        Windows = 2,
    }
}
