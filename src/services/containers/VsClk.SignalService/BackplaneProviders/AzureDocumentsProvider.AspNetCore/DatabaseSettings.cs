// <copyright file="DatabaseSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    public class DatabaseSettings
    {
        public string EndpointUrl { get; set; }

        public string AuthorizationKey { get; set; }

        public bool IsProduction { get; set; }
    }
}
