using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace Microsoft.VsCloudKernel.SignalService.ServiceHubTests
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

        public static IHubContext<T> CreateHubContextMock<T>(Dictionary<string, IClientProxy> clientProxies)
            where T : Hub
        {
            var mockGroupManager = new MockGroupManager();
            var mockHubClients = new Mock<IHubClients>();
            mockHubClients.Setup(i => i.Client(It.IsAny<string>())).Returns((string connectionId) =>
            {
                IClientProxy clientProxy;
                clientProxies.TryGetValue(connectionId, out clientProxy);
                return clientProxy;
            });

            mockHubClients.Setup(i => i.Group(It.IsAny<string>())).Returns((string groupName) =>
            {
                var proxies = mockGroupManager.Groups[groupName].Select(connId => clientProxies[connId]);
                var mockClient = new Mock<IClientProxy>();
                mockClient.Setup(e => e.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                        .Returns((string method, object[] args, CancellationToken cancellationToken) =>
                        {
                            return Task.WhenAll(proxies.Select(p => p.SendCoreAsync(method, args, cancellationToken)));
                        });
                return mockClient.Object;
            });

            var hubContextMock = new Mock<IHubContext<T>>();
            hubContextMock.Setup(i => i.Clients).Returns(mockHubClients.Object);
            hubContextMock.Setup(i => i.Groups).Returns(mockGroupManager);

            return hubContextMock.Object;
        }

        public static IHubContextHost CreateHubContextHostMock<THub>(Dictionary<string, IClientProxy> clientProxies) where THub : Hub
        {
            return new HubContextHost<THub, THub>(CreateHubContextMock<THub>(clientProxies));
        }

        public static IEnumerable<IHubContextHost> CreateSingleHubContextHostMock<THub>(Dictionary<string, IClientProxy> clientProxies) where THub : Hub
        {
            return new IHubContextHost[] { CreateHubContextHostMock<THub>(clientProxies) };
        }

        private class MockGroupManager : IGroupManager
        {
            public Dictionary<string, HashSet<string>> Groups { get; } = new Dictionary<string, HashSet<string>>();

            public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
            {
                HashSet<string> connections;
                if (!Groups.TryGetValue(groupName, out connections))
                {
                    connections = new HashSet<string>();
                    Groups[groupName] = connections;
                }

                connections.Add(connectionId);
                return Task.CompletedTask;
            }

            public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
            {
                if (Groups.TryGetValue(groupName, out var connections))
                {
                    connections.Remove(connectionId);
                }

                return Task.CompletedTask;
            }
        }
    
    
    }
}
