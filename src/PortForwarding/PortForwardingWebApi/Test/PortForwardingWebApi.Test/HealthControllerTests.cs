using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Controllers;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Test
{
    public class HealthControllerTest
    {
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly IDiagnosticsLogger logger; 

        public HealthControllerTest()
        {
            loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();
        }

        [Fact]
        public void HealthController_Constructor()
        {
            var healthProviderMock = new Mock<IHealthProvider>();
            var controller = CreateHealthController(healthProviderMock.Object);

            Assert.NotNull(controller);
        }

        [Fact]
        public void HealthController_Get_Healthy()
        {
            var healthProviderMock = new Mock<IHealthProvider>();
            healthProviderMock.Setup(p => p.IsHealthy).Returns(true);
            var controller = CreateHealthController(healthProviderMock.Object);

            var result = controller.Get();

            if (result is StatusCodeResult statusCodeResult)
            {
                Assert.Equal(200, statusCodeResult.StatusCode);
            }
        }

        [Fact]
        public void HealthController_Get_Unhealthy()
        {
            var healthProviderMock = new Mock<IHealthProvider>();
            healthProviderMock.Setup(p => p.IsHealthy).Returns(false);
            var controller = CreateHealthController(healthProviderMock.Object);

            var result = controller.Get();

            if (result is StatusCodeResult statusCodeResult)
            {
                Assert.Equal(500, statusCodeResult.StatusCode);
            }
        }

        private HealthController CreateHealthController(IHealthProvider healthProvider)
        {
            var controller = new HealthController(healthProvider);

            var httpContext = MockHttpContext.Create();
            var logger = new Mock<IDiagnosticsLogger>().Object;
            httpContext.SetLogger(logger);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
            };

            return controller;
        }
    }
}
