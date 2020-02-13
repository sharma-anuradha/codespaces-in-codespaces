﻿// <copyright file="IBackplaneServiceDataProvider.cs" company="Microsoft">
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
        string[] ActiveServices { get; set; }

        Task<bool> ContainsContactAsync(string contactId, CancellationToken cancellationToken);

        Task UpdateContactDataInfoAsync(string contactId, ContactDataInfo contactDataInfo, CancellationToken cancellationToken);
    }
}
