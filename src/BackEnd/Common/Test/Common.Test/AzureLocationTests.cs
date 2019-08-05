// <copyright file="AzureLocationTests.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Test
{
    public class AzureLocationTests
    {
        [Fact]
        public void UniqueValues()
        {
            // Ensure that no two AzureLocation entries have the same value
            var values = Enum.GetValues(typeof(AzureLocation));
            var valuesSet = new HashSet<int>();
            foreach (var value in values)
            {
                var intValue = (int)value;
                Assert.DoesNotContain(intValue, valuesSet);
                valuesSet.Add(intValue);
            }
        }
    }
}
