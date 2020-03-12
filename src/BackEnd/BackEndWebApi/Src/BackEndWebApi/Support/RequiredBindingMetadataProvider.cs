// <copyright file="RequiredBindingMetadataProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApi.Support
{
    /// <summary>
    /// Required Binding Metadata Provider.
    /// </summary>
    public class RequiredBindingMetadataProvider : IBindingMetadataProvider
    {
        /// <inheritdoc/>
        public void CreateBindingMetadata(BindingMetadataProviderContext context)
        {
            if (context.PropertyAttributes?.OfType<RequiredAttribute>().Any() == true)
            {
                context.BindingMetadata.IsBindingRequired = true;
            }
        }
    }
}