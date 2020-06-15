// <copyright file="PcfExportContractResolver.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Linq;
using System.Reflection;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PCFAgent
{
    /// <summary>
    /// PcfExportContractResolver.
    /// </summary>
    public class PcfExportContractResolver : DefaultContractResolver
    {
        /// <summary>
        /// Make the propery exportable only if it has GDPR attribute with Action = GDPRAction.Export.
        /// </summary>
        /// <param name="member">member.</param>
        /// <param name="memberSerialization">member serialization.</param>
        /// <returns>Updated property.</returns>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            property.ShouldSerialize =
                    instance =>
                    {
                        return property.AttributeProvider.GetAttributes(typeof(GDPR), false).Any(attribute => attribute is GDPR gdpr && gdpr.Action == GDPRAction.Export);
                    };

            return property;
        }
    }
}
