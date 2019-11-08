using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsCloudKernel.SignalService;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// Interface to define a contract for a contact backplane notification
    /// </summary>
    public interface IContactBackplaneServiceNotification
    {
        Task FireOnUpdateContactAsync(ContactDataChanged<ContactDataInfo> contactDataChanged, string[] affectedProperties, CancellationToken cancellationToken);
        Task FireOnSendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken);
    }
}
