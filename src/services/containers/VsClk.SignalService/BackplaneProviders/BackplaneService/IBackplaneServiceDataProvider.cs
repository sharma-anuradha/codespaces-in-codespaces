// <copyright file="IBackplaneServiceDataProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsCloudKernel.SignalService;
using ContactDataInfo = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>>;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    public interface IBackplaneServiceDataProvider : IContactBackplaneDataProvider
    {
        int TotalContacts { get; }

        int TotalConnections { get; }

        Task<bool> ContainsContactAsync(string contactId, CancellationToken cancellationToken);

        Task<ContactDataInfo> UpdateRemoteDataInfoAsync(
            string contactId,
            ContactDataInfo remoteDataInfo,
            CancellationToken cancellationToken);

        Task<(ContactDataInfo NewValue, ContactDataInfo OldValue)> UpdateLocalDataChangedAsync<T>(
                ContactDataChangedRef<T> localDataChangedRef,
                string[] localServices,
                CancellationToken cancellationToken)
            where T : class;
    }
}
