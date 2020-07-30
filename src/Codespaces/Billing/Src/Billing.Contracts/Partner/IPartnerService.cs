// <copyright file="IPartnerService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Interface for gitHub submission functionality.
    /// </summary>
    public interface IPartnerService
    {
        /// <summary>
        /// Push gitHub queue submission.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>Task to track completion.</returns>
        Task Execute(CancellationToken cancellationToken);
    }
}