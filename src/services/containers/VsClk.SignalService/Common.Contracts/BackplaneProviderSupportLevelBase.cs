// <copyright file="BackplaneProviderSupportLevelBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// A backplane provider support level class to define features being supported.
    /// </summary>
    public class BackplaneProviderSupportLevelBase
    {
        public int? UpdateMetrics { get; set; }

        public int? DisposeDataChanges { get; set; }
    }
}
