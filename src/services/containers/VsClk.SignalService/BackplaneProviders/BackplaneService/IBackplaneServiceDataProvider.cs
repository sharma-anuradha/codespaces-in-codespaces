using Microsoft.VsCloudKernel.SignalService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    public interface IBackplaneServiceDataProvider : IContactBackplaneDataProvider
    {
        string[] ActiveServices { get; set; }

        Task<bool> ContainsContactAsync(string contactId, CancellationToken cancellationToken);
        Task UpdateContactDataInfoAsync(string contactId, ContactDataInfo contactDataInfo, CancellationToken cancellationToken);
    }
}
