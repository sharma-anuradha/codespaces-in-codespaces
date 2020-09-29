// <copyright file="IConfigurationScopeGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator
{
    /// <summary>
    /// Marker interface for the Configuration scope generator. For details please visit this wiki page - https://github.com/microsoft/vssaas-planning/wiki/Configuration-Reader
    /// </summary>
    public interface IConfigurationScopeGenerator
    {        
        /// <summary>
        /// Generates a list of configuration scopes applicable for a given context object.
        /// </summary>
        /// <param name="context">Context describing applicable scopes for the configuration.</param>
        /// <returns>A list of <see cref="string"/> representing generated scopes based on the provided context. The list will have scopes in decreasing order of priority i.e
        /// first item will have the highest priority whereas the last element will have least priority.</returns>
        IEnumerable<string> GetScopes(ConfigurationContext context);
    }
}
