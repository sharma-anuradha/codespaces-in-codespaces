// <copyright file="DeploymentStatusInputExtension.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    public static class DeploymentStatusInputExtension
    {
        public static string ToJson(this DeploymentStatusInput input)
        {
            return JsonConvert.SerializeObject(input);
        }

        public static DeploymentStatusInput ToDeploymentStatusInput(this string input)
        {
            return JsonConvert.DeserializeObject<DeploymentStatusInput>(input);
        }
    }
}