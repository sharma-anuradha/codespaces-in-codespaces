// <copyright file="LocationExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Runtime.InteropServices;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    public static class LocationExtensions
    {
        public static DateTime GetTimeInLocation(this AzureLocation location, DateTime utcTime)
        {
            switch (location)
            {
                case AzureLocation.EastUs:
                    return utcTime.AddHours(-4);
                case AzureLocation.SouthEastAsia:
                    return utcTime.AddHours(8);
                case AzureLocation.WestEurope:
                    return utcTime.AddHours(2);
                case AzureLocation.WestUs2:
                    return utcTime.AddHours(-7);
                default:
                    return utcTime;
            }
        }
    }
}
