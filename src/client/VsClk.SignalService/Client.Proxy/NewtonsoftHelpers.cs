// <copyright file="NewtonsoftHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsCloudKernel.SignalService.Client
{
    internal static class NewtonsoftHelpers
    {
        public static Dictionary<string, Dictionary<string, object>> ToPropertyDictionary(this JObject jObject)
        {
            return ((IDictionary<string, JToken>)jObject).ToDictionary(kvp => kvp.Key, kvp => ((IDictionary<string, JToken>)kvp.Value).ToDictionary(kvp2 => kvp2.Key, kvp2 => ToObject(kvp2.Value)));
        }

        public static object ToObject(object value)
        {
            if (value is JToken jToken && jToken.Type != JTokenType.Object)
            {
                return jToken.ToObject<object>();
            }

            return value;
        }

        public static object ToObject(this JToken jToken)
        {
            return jToken?.Type != JTokenType.Object ? jToken?.ToObject<object>() : jToken;
        }
    }
}
