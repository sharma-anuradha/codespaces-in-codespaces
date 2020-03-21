// <copyright file="ResourceProxyDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Resource Details.
    /// </summary>
    public class ResourceProxyDetails
    {
        private const string ImageFamilyNameName = "imageFamilyName";
        private const string ImageNameName = "imageName";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceProxyDetails"/> class.
        /// </summary>
        /// <param name="record">Target record.</param>
        public ResourceProxyDetails(ResourceRecord record)
        {
            Record = record;
        }

        /// <summary>
        /// Gets the underling azure location.
        /// </summary>
        public AzureLocation Location
        {
            get { return (AzureLocation)Enum.Parse(typeof(AzureLocation), Record.Location, true); }
        }

        /// <summary>
        /// Gets the underling azure location.
        /// </summary>
        public string SkuName
        {
            get { return Record.SkuName; }
        }

        /// <summary>
        /// Gets the underling image family name.
        /// </summary>
        public string ImageFamilyName
        {
            get { return Record?.PoolReference?.Dimensions.GetValueOrDefault(ImageFamilyNameName); }
        }

        /// <summary>
        /// Gets the underling image name.
        /// </summary>
        public string ImageName
        {
            get { return Record?.PoolReference?.Dimensions.GetValueOrDefault(ImageNameName); }
        }

        /// <summary>
        /// Gets the backing resource record.
        /// </summary>
        protected ResourceRecord Record { get; }
    }
}
