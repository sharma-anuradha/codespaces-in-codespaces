// <copyright file="IContactBackplaneServiceNotification.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsCloudKernel.SignalService;
using ContactDataInfo = System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IDictionary<string, Microsoft.VsCloudKernel.SignalService.PropertyValue>>>;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    /// <summary>
    /// Interface to define a contract for a contact backplane notification
    /// </summary>
    public interface IContactBackplaneServiceNotification
    {
        Task FireOnUpdateContactAsync(ContactDataChanged<ContactDataInfo> contactDataChanged, string[] affectedProperties, CancellationToken cancellationToken);

        Task FireOnSendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken);
    }
}
