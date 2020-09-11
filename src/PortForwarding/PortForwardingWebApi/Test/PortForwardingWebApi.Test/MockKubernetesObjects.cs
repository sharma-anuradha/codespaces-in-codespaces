using k8s.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Test
{
    class MockKubernetesObjects
    {
        public static V1Endpoints Endpoint => new V1Endpoints
            {
                Metadata = new V1ObjectMeta
                {
                    Name = "test-endpoint",
                },
                Subsets = new[]
                {
                    new V1EndpointSubset
                    {
                        Addresses = new[]
                        {
                            new V1EndpointAddress { Ip = "0.0.0.0" },
                        },
                        Ports = new[]
                        {
                            new V1EndpointPort(443, name: "https-443"),
                        },
                    },
                },
            };
    }
}
