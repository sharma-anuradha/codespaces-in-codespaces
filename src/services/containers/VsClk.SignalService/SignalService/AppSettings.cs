// <copyright file="AppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsCloudKernel.SignalService
{
    public class AppSettings : AppSettingsBase
    {
        public string BaseUri { get; set; }

        public string AuthenticateProfileServiceUri { get; set; }

        public string BackplaneHostName { get; set; }

        public string KeyVaultName { get; set; }

        public string[] CorsOrigin { get; set; }
    }
}
