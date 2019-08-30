// <copyright file="NextStageInputExtension.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public static class NextStageInputExtension
    {
        public static string ToJson(this NextStageInput input)
        {
            return JsonConvert.SerializeObject(input);
        }

        public static NextStageInput ToNextStageInput(this string input)
        {
            return JsonConvert.DeserializeObject<NextStageInput>(input);
        }
    }
}