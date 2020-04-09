using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Partners;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Tokens;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public class VsSaaSTokenTests
    {
        private const string DefaultProviderId = "vso";

        private static readonly VsoPlan TestPlan = new VsoPlan
        {
            Id = "mock-plan",
            Plan = new VsoPlanInfo
            {
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "mock-resource-group",
                Name = "mock-name",
            },
        };

        private static readonly string[] TestScopes = new[] 
        {
            "mock-scope-1", 
            "mock-scope-2",
        };

        private static readonly DelegateIdentity TestIdentityWithoutEmail = new DelegateIdentity
        {
            Id = "mock-id",
            Username = "mock-username",
            DisplayName = "mock-display-name",
        };

        private static readonly DelegateIdentity TestIdentityWithEmail = new DelegateIdentity
        {
            Id = "mock-id",
            Username = "mock-username@email.com",
            DisplayName = "mock-display-name",
        };

        private static readonly AuthenticationSettings TestAuthSettings = new AuthenticationSettings
        {
            VsSaaSTokenSettings = new TokenSettings
            {
                Issuer = "mock-iss",
                Audience = "mock-aud",
                Lifetime = null,
            },
        };

        private const string TestTokenValue = "mock-token";

        private static readonly IDiagnosticsLogger TestLogger = new JsonStdoutLogger(new LogValueSet());

        [Fact]
        public void GenerateDelegatedToken_DefaultPartnerWithoutEmail()
        {
            var identity = TestIdentityWithoutEmail;

            var mockWriter = new Mock<IJwtWriter>();
            mockWriter
                .Setup(x => x.WriteToken(It.IsAny<JwtPayload>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(TestTokenValue)
                .Callback((JwtPayload payload, IDiagnosticsLogger logger) =>
                {
                    AssertHasClaimWithValue(payload, CustomClaims.Provider, DefaultProviderId);
                    AssertHasClaimWithValue(payload, CustomClaims.TenantId, TestPlan.Id);
                    AssertHasClaimWithValue(payload, CustomClaims.OId, identity.Id);
                    AssertHasClaimWithValue(payload, CustomClaims.Username, identity.Username);
                    AssertHasClaimWithValue(payload, CustomClaims.DisplayName, identity.DisplayName);
                    AssertHasClaimWithValue(payload, CustomClaims.PlanResourceId, TestPlan.Plan.ResourceId);
                    AssertHasClaimWithValue(payload, CustomClaims.Scope, string.Join(" ", TestScopes));

                    Assert.False(payload.TryGetValue(CustomClaims.Email, out var _));
                });

            var mockProvider = new Mock<ITokenProvider>();
            mockProvider.Setup(x => x.JwtWriter).Returns(mockWriter.Object);
            mockProvider.Setup(x => x.Settings).Returns(TestAuthSettings);

            var token = mockProvider.Object.GenerateDelegatedVsSaaSToken(
                TestPlan, null, TestScopes, identity, null, null, null, TestLogger);

            Assert.Equal(TestTokenValue, token);
        }

        [Fact]
        public void GenerateDelegatedToken_DefaultPartnerWithEmail()
        {
            var identity = TestIdentityWithEmail;

            var mockWriter = new Mock<IJwtWriter>();
            mockWriter
                .Setup(x => x.WriteToken(It.IsAny<JwtPayload>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(TestTokenValue)
                .Callback((JwtPayload payload, IDiagnosticsLogger logger) =>
                {
                    AssertHasClaimWithValue(payload, CustomClaims.Provider, DefaultProviderId);
                    AssertHasClaimWithValue(payload, CustomClaims.TenantId, TestPlan.Id);
                    AssertHasClaimWithValue(payload, CustomClaims.OId, identity.Id);
                    AssertHasClaimWithValue(payload, CustomClaims.Username, identity.Username);
                    AssertHasClaimWithValue(payload, CustomClaims.DisplayName, identity.DisplayName);
                    AssertHasClaimWithValue(payload, CustomClaims.PlanResourceId, TestPlan.Plan.ResourceId);
                    AssertHasClaimWithValue(payload, CustomClaims.Scope, string.Join(" ", TestScopes));
                    AssertHasClaimWithValue(payload, CustomClaims.Email, identity.Username);
                });

            var mockProvider = new Mock<ITokenProvider>();
            mockProvider.Setup(x => x.JwtWriter).Returns(mockWriter.Object);
            mockProvider.Setup(x => x.Settings).Returns(TestAuthSettings);

            var token = mockProvider.Object.GenerateDelegatedVsSaaSToken(
                TestPlan, null, TestScopes, identity, null, null, null, TestLogger);

            Assert.Equal(TestTokenValue, token);
        }

        [Fact]
        public void GenerateDelegatedToken_GitHubPartnerWithEmail()
        {
            var identity = TestIdentityWithEmail;

            var mockWriter = new Mock<IJwtWriter>();
            mockWriter
                .Setup(x => x.WriteToken(It.IsAny<JwtPayload>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(TestTokenValue)
                .Callback((JwtPayload payload, IDiagnosticsLogger logger) =>
                {
                    AssertHasClaimWithValue(payload, CustomClaims.Provider, "github");
                    AssertHasClaimWithValue(payload, CustomClaims.TenantId, TestPlan.Id);
                    AssertHasClaimWithValue(payload, CustomClaims.OId, identity.Id);
                    AssertHasClaimWithValue(payload, CustomClaims.Username, identity.Username);
                    AssertHasClaimWithValue(payload, CustomClaims.DisplayName, identity.DisplayName);
                    AssertHasClaimWithValue(payload, CustomClaims.PlanResourceId, TestPlan.Plan.ResourceId);
                    AssertHasClaimWithValue(payload, CustomClaims.Scope, string.Join(" ", TestScopes));
                    AssertHasClaimWithValue(payload, CustomClaims.Email, identity.Username);
                });

            var mockProvider = new Mock<ITokenProvider>();
            mockProvider.Setup(x => x.JwtWriter).Returns(mockWriter.Object);
            mockProvider.Setup(x => x.Settings).Returns(TestAuthSettings);

            var token = mockProvider.Object.GenerateDelegatedVsSaaSToken(
                TestPlan, Partner.GitHub, TestScopes, identity, null, null, null, TestLogger);

            Assert.Equal(TestTokenValue, token);
        }

        [Fact]
        public void GenerateDelegatedToken_GitHubPartnerWithoutEmail()
        {
            var identity = TestIdentityWithoutEmail;

            var mockWriter = new Mock<IJwtWriter>();
            mockWriter
                .Setup(x => x.WriteToken(It.IsAny<JwtPayload>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(TestTokenValue)
                .Callback((JwtPayload payload, IDiagnosticsLogger logger) =>
                {
                    AssertHasClaimWithValue(payload, CustomClaims.Provider, "github");
                    AssertHasClaimWithValue(payload, CustomClaims.TenantId, TestPlan.Id);
                    AssertHasClaimWithValue(payload, CustomClaims.OId, identity.Id);
                    AssertHasClaimWithValue(payload, CustomClaims.Username, identity.Username);
                    AssertHasClaimWithValue(payload, CustomClaims.DisplayName, identity.DisplayName);
                    AssertHasClaimWithValue(payload, CustomClaims.PlanResourceId, TestPlan.Plan.ResourceId);
                    AssertHasClaimWithValue(payload, CustomClaims.Scope, string.Join(" ", TestScopes));
                    AssertHasClaimWithValue(payload, CustomClaims.Email, identity.Username + "@users.noreply.github.com");
                });

            var mockProvider = new Mock<ITokenProvider>();
            mockProvider.Setup(x => x.JwtWriter).Returns(mockWriter.Object);
            mockProvider.Setup(x => x.Settings).Returns(TestAuthSettings);

            var token = mockProvider.Object.GenerateDelegatedVsSaaSToken(
                TestPlan, Partner.GitHub, TestScopes, identity, null, null, null, TestLogger);

            Assert.Equal(TestTokenValue, token);
        }

        [Fact]
        public void GenerateDelegatedToken_WithEnvironmentIds()
        {
            var environmentIds = new[]
            {
                Guid.Empty.ToString(),
                Guid.Empty.ToString().Replace('0', '1'),
            };

            var identity = TestIdentityWithEmail;

            var mockWriter = new Mock<IJwtWriter>();
            mockWriter
                .Setup(x => x.WriteToken(It.IsAny<JwtPayload>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(TestTokenValue)
                .Callback((JwtPayload payload, IDiagnosticsLogger logger) =>
                {
                    AssertHasClaimWithValue(payload, CustomClaims.Provider, "github");
                    AssertHasClaimWithValue(payload, CustomClaims.TenantId, TestPlan.Id);
                    AssertHasClaimWithValue(payload, CustomClaims.OId, identity.Id);
                    AssertHasClaimWithValue(payload, CustomClaims.PlanResourceId, TestPlan.Plan.ResourceId);
                    AssertHasClaimWithValue(payload, CustomClaims.Scope, string.Join(" ", TestScopes));
                    AssertHasClaimWithValue(payload, CustomClaims.Environments, environmentIds);
                });

            var mockProvider = new Mock<ITokenProvider>();
            mockProvider.Setup(x => x.JwtWriter).Returns(mockWriter.Object);
            mockProvider.Setup(x => x.Settings).Returns(TestAuthSettings);

            var token = mockProvider.Object.GenerateDelegatedVsSaaSToken(
                TestPlan, Partner.GitHub, TestScopes, identity, null, null, environmentIds, TestLogger);

            Assert.Equal(TestTokenValue, token);
        }

        private static void AssertHasClaimWithValue(JwtPayload payload, string claimName, string expectedValue)
        {
            Assert.True(payload.TryGetValue(claimName, out var actualValue));
            Assert.Equal(expectedValue, actualValue);
        }

        private static void AssertHasClaimWithValue(JwtPayload payload, string claimName, string[] expectedValue)
        {
            Assert.True(payload.TryGetValue(CustomClaims.Environments, out var actualValue));
            Assert.Equal(expectedValue, actualValue as IEnumerable<object>);
        }
    }
}
