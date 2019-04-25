using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Microsoft.VsCloudKernel.SignalService.PresenceServiceHubTests
{
    internal static class MockUtils
    {
        public static IClientProxy CreateClientProxy(Func<string, object[], Task> callback)
        {
            var mockClient = new Mock<IClientProxy>();
            mockClient.Setup(e => e.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                    .Returns((string method, object[] args, CancellationToken cancellationToken) => callback(method, args));
            return mockClient.Object;
        }

        public static IHubContext<PresenceServiceHub> CreateHubContextMock(Dictionary<string, IClientProxy> clientProxies)
        {
            var mockHubClients = new Mock<IHubClients>();
            mockHubClients.Setup(i => i.Client(It.IsAny<string>())).Returns((string connectionId) =>
            {
                IClientProxy clientProxy;
                clientProxies.TryGetValue(connectionId, out clientProxy);
                return clientProxy;
            });

            var hubContextMock = new Mock<IHubContext<PresenceServiceHub>>();
            hubContextMock.Setup(i => i.Clients).Returns(mockHubClients.Object);

            return hubContextMock.Object;
        }
    }
}
