using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService;

namespace Microsoft.VsCloudKernel.BackplaneService
{
    using ConnectionProperties = IDictionary<string, PropertyValue>;
    using ContactDataInfo = IDictionary<string, IDictionary<string, IDictionary<string, PropertyValue>>>;

    /// <summary>
    /// A hub to expose our backplane service public methods
    /// </summary>
    public class ContactBackplaneHub : Hub
    {
        private const int ServiceIdKey = 1;

        public ContactBackplaneHub(ContactBackplaneService backplaneService, ILogger<ContactBackplaneHub> logger)
        {
            BackplaneService = Requires.NotNull(backplaneService, nameof(backplaneService));
            Logger = Requires.NotNull(logger, nameof(logger));
        }

        private ILogger Logger { get; }
        private ContactBackplaneService BackplaneService { get; }

        public void RegisterService(string serviceId)
        {
            Context.Items[ServiceIdKey] = serviceId;
            BackplaneService.RegisterService(serviceId);
        }

        public Task UpdateMetricsAsync((string ServiceId, string Stamp) serviceInfo, ContactServiceMetrics metrics) =>
            BackplaneService.UpdateMetricsAsync(serviceInfo, metrics, Context.ConnectionAborted);

        public Task UpdateContactAsync(ContactDataChanged<ConnectionProperties> contactDataChanged) =>
            BackplaneService.UpdateContactAsync(contactDataChanged, Context.ConnectionAborted);

        public Task<ContactDataInfo> GetContactDataAsync(string contactId) =>
            BackplaneService.GetContactDataAsync(contactId, Context.ConnectionAborted);

        public Task<Dictionary<string, ContactDataInfo>> GetContactsDataAsync(Dictionary<string, object> matchProperties) =>
            BackplaneService.GetContactsDataAsync(matchProperties, Context.ConnectionAborted);

        public Task SendMessageAsync(string sourceId, MessageData messageData) =>
            BackplaneService.SendMessageAsync(sourceId, messageData, Context.ConnectionAborted);

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            BackplaneService.OnDisconnected(GetContextServiceId(), exception);
            return base.OnDisconnectedAsync(exception);
        }

        private string GetContextServiceId()
        {
            object serviceId;
            if (Context.Items.TryGetValue(ServiceIdKey, out serviceId))
            {
                return serviceId.ToString();
            }

            return null;
        }

    }
}
