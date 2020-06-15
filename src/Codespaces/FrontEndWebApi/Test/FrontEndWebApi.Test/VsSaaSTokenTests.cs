using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
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
        public async Task GenerateDelegatedToken_DefaultPartnerWithoutEmail()
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

            var mockProvider = MockTokenProvider(mockWriter.Object);
            var token = await mockProvider.GenerateDelegatedVsSaaSTokenAsync(
                TestPlan, null, TestScopes, identity, null, null, null, TestLogger);

            Assert.Equal(TestTokenValue, token);
        }

        [Fact]
        public async Task GenerateDelegatedToken_DefaultPartnerWithEmail()
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

            var mockProvider = MockTokenProvider(mockWriter.Object);
            var token = await mockProvider.GenerateDelegatedVsSaaSTokenAsync(
                TestPlan, null, TestScopes, identity, null, null, null, TestLogger);

            Assert.Equal(TestTokenValue, token);
        }

        [Fact]
        public async Task GenerateDelegatedToken_GitHubPartnerWithEmail()
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

            var mockProvider = MockTokenProvider(mockWriter.Object);
            var token = await mockProvider.GenerateDelegatedVsSaaSTokenAsync(
                TestPlan, Partner.GitHub, TestScopes, identity, null, null, null, TestLogger);

            Assert.Equal(TestTokenValue, token);
        }

        [Fact]
        public async Task GenerateDelegatedToken_GitHubPartnerWithoutEmail()
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

            var mockProvider = MockTokenProvider(mockWriter.Object);
            var token = await mockProvider.GenerateDelegatedVsSaaSTokenAsync(
                TestPlan, Partner.GitHub, TestScopes, identity, null, null, null, TestLogger);

            Assert.Equal(TestTokenValue, token);
        }

        [Fact]
        public async Task GenerateDelegatedToken_WithEnvironmentIds()
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

            var mockProvider = MockTokenProvider(mockWriter.Object);
            var token = await mockProvider.GenerateDelegatedVsSaaSTokenAsync(
                TestPlan, Partner.GitHub, TestScopes, identity, null, null, environmentIds, TestLogger);

            Assert.Equal(TestTokenValue, token);
        }

        private static ITokenProvider MockTokenProvider(IJwtWriter jwtWriter)
        {
            var mockProvider = new Mock<ITokenProvider>();
            mockProvider.Setup(x => x.Settings).Returns(TestAuthSettings);
            mockProvider.Setup(obj => obj.IssueTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<IEnumerable<Claim>>(),
                It.IsAny<IDiagnosticsLogger>()))
                .Returns((
                    string issuer,
                    string audience,
                    DateTime expires,
                    IEnumerable<Claim> claims,
                    IDiagnosticsLogger logger)
                    => Task.FromResult(jwtWriter.WriteToken(
                        logger, issuer, audience, expires, claims.ToArray())));
            return mockProvider.Object;
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
