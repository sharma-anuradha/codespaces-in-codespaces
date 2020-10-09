// <copyright file="LocationTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test
{
    public class LocationTests
    {
        [Theory]
        [InlineData(AzureLocation.WestUs2, -7)]
        [InlineData(AzureLocation.EastUs, -4)]
        [InlineData(AzureLocation.WestEurope, 2)]
        [InlineData(AzureLocation.SouthEastAsia, 8)]
        [InlineData(AzureLocation.NorthEurope, 0)]
        [InlineData(AzureLocation.WestIndia, 0)]
        public void AzureLocation_TimeInLocation(AzureLocation location, int timeDiff)
        {
            var utcTime = DateTime.UtcNow;
            var currentTime = location.GetTimeInLocation(utcTime);
            var difference = currentTime - utcTime;

            Assert.True(difference.Hours == timeDiff);
        }
    }
}
