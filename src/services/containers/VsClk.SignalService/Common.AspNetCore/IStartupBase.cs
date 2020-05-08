// <copyright file="IStartupBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    public interface IStartupBase
    {
        string Environment { get; }

        bool IsDevelopmentEnv { get; }

        ServiceInfo ServiceInfo { get; }
    }
}
