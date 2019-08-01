// <copyright file="FileShareProviderCreateResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class FileShareProviderCreateResult
    {
        // Provider generates what ever ID it wants... might be worth versioning this?
        public string ResourceId { get; set; } // '/subscriptions/2fa47206-c4b5-40ff-a5e6-9160f9ee000c/storage/<uid>'
        public string TrackingId { get; set; } // Under the covers maps to ARM Deployment id
    }
}
