// <copyright file="BackplaneProviderSupportLevelConst.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Default value for backplane providers.
    /// </summary>
    public static class BackplaneProviderSupportLevelConst
    {
        public const int DefaultSupportThreshold = 10;

        public const int MinimumSupportThreshold = 1;

        public const int NoSupportThreshold = 0;
    }
}
