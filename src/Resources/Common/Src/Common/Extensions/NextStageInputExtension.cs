// <copyright file="NextStageInputExtension.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common
{
    /// <summary>
    /// Next stage input convertors.
    /// </summary>
    public static class NextStageInputExtension
    {
        /// <summary>
        /// Conver to Json.
        /// </summary>
        /// <param name="input">input.</param>
        /// <returns>result.</returns>
        public static string ToJson(this NextStageInput input)
        {
            return JsonConvert.SerializeObject(input);
        }

        /// <summary>
        /// Convert to object.
        /// </summary>
        /// <param name="input">input.</param>
        /// <returns>result.</returns>
        public static NextStageInput ToNextStageInput(this string input)
        {
            return JsonConvert.DeserializeObject<NextStageInput>(input);
        }
    }
}