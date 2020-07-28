// <copyright file="ProfileProgram.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

#pragma warning disable SA1600 // Elements should be documented
namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.Models.PrivatePreview
{
    /// <summary>
    /// Copy of https://devdiv.visualstudio.com/DevDiv/_git/Cascade?path=%2Fsrc%2FServices%2FCollaboration.Contracts%2FProfileProgram.cs.
    /// </summary>
    public class ProfileProgram
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "items")]
        public Dictionary<string, object> Items { get; set; }

        public T GetItem<T>(string key)
        {
            if (!Items.TryGetValue(key, out var value))
            {
                return default;
            }

            return value is T ? (T)value : default;
        }
    }
}
#pragma warning restore SA1600 // Elements should be documented