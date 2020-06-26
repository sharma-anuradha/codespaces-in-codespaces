// <copyright file="ComponentExtension.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common
{
    /// <summary>
    /// Capacity manager extension.
    /// </summary>
    public static class ComponentExtension
    {
        /// <summary>
        /// Convert component list to dictionary.
        /// </summary>
        /// <param name="customComponents">component list.</param>
        /// <returns>result.</returns>
        public static Dictionary<string, ResourceComponent> ToComponentDictionary(this IList<ResourceComponent> customComponents)
        {
            return customComponents?.ToDictionary(c =>
            {
                if (c is null)
                {
                    throw new ArgumentNullException($"ResourceComponent is null in {customComponents}.");
                }

                if (string.IsNullOrEmpty(c.ComponentId))
                {
                    throw new ArgumentNullException($"ResourceComponent id is null or empty for {c.ComponentType}.");
                }

                return c.ComponentId;
            });
        }
    }
}