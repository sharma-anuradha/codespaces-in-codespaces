// <copyright file="CreateResourceContinuationInputOptionsConverter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models
{
    /// <summary>
    /// Json converter for CreateResourceContinuationInputOptions type
    /// </summary>
    public class CreateResourceContinuationInputOptionsConverter : JsonTypeConverter
    {
        private static readonly Dictionary<string, Type> MapTypes
                = new Dictionary<string, Type>
            {
                { "computeVM", typeof(CreateComputeContinuationInputOptions) },
            };

        protected override Type BaseType => typeof(CreateResourceContinuationInputOptions);

        protected override IDictionary<string, Type> SupportedTypes => MapTypes;
    }
}
