// <copyright file="IUserSubscriptionRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Azure.Storage.DocumentDB;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserSubscriptions
{
    /// <summary>
    /// A repository of <see cref="CloudEnvironment"/>.
    /// </summary>
    public interface IUserSubscriptionRepository : IDocumentDbCollection<UserSubscription>
    {
    }
}
