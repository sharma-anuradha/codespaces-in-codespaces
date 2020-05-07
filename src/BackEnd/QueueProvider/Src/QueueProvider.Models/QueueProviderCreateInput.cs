// <copyright file="QueueProviderCreateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Models
{
    public class QueueProviderCreateInput : ContinuationInput
    {
        public AzureLocation AzureLocation { get; set; }

        public string QueueName { get; set; }
    }
}