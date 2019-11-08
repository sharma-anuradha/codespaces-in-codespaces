using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VsCloudKernel.SignalService;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    public class HubContactBackplaneServiceNotification : IContactBackplaneServiceNotification
    {
        public HubContactBackplaneServiceNotification(
            IHubContext<ContactBackplaneHub> backplaneHubContext)
        {
            BackplaneHubContext = Requires.NotNull(backplaneHubContext, nameof(backplaneHubContext));
        }

        private IHubContext<ContactBackplaneHub> BackplaneHubContext { get; }

        public async Task FireOnUpdateContactAsync(ContactDataChanged<ContactDataInfo> contactDataChanged, string[] affectedProperties, CancellationToken cancellationToken)
        {
            await BackplaneHubContext.Clients.All.SendAsync("OnUpdateContact", contactDataChanged, affectedProperties, cancellationToken);
        }

        public async Task FireOnSendMessageAsync(string sourceId, MessageData messageData, CancellationToken cancellationToken)
        {
            await BackplaneHubContext.Clients.All.SendAsync("OnSendMessage", sourceId, messageData, cancellationToken);
        }
    }
}
