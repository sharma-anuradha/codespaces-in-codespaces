// <copyright file="ServiceInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    public struct ServiceInfo
    {
        public ServiceInfo(string serviceId, string stamp, string serviceType)
        {
            ServiceId = serviceId;
            Stamp = stamp;
            ServiceType = serviceType;
        }

        public string ServiceId { get; }

        public string Stamp { get; }

        public string ServiceType { get; }
    }
}
