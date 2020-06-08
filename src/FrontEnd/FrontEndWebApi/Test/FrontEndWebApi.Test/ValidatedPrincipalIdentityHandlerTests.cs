using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.IdentityMap;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Moq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public class ValidatedPrincipalIdentityHandlerTests
    {
        private const string Base64DummyToken = "ZXlKaGJHY2lPaUpJVXpJMU5pSXNJblI1Y0NJNklrcFhWQ0o5LmV5SnpkV0lpT2lJeE1qTTBOVFkzT0Rrd0lpd2libUZ0WlNJNklrcHZhRzRnUkc5bElpd2lhV0YwSWpveE5URTJNak01TURJeWZRLlNmbEt4d1JKU01lS0tGMlFUNGZ3cE1lSmYzNlBPazZ5SlZfYWRRc3N3NWM=";
        private readonly string DummyToken = System.Text.Encoding.Default.GetString(System.Convert.FromBase64String(Base64DummyToken));

        public class TestCase
        {
            public ClaimsIdentity TestIdentity { get; set; }

            public string ExpectedHttpContextPlan { get; set; }

            public IEnumerable<string> ExpectedHttpContextScopes { get; set; }
        }

        public static readonly TheoryData<TestCase> Identities = new TheoryData<TestCase>
        {
            { MockVsClaims() },
            { MockAadClaims() },

            { MockCascadeClaims("plan-resource-id", new[] { "read:allenvironments" }) },
            { MockCascadeClaims("plan-resource-id", new[] { "read:allenvironments", "write:environments" })},

            { MockDelegateCascadeClaims("plan-resource-id", new[] { "read:allenvironments" }) },
            { MockDelegateCascadeClaims("plan-resource-id", new[] { "read:allenvironments", "write:environments" }) },
        };

        [Theory]
        [MemberData(nameof(Identities))]
        public async Task ValidatedPrincipalAsync(TestCase testCase)
        {
            var httpContext = MockHttpContext.Create();

            var currentUser = new MockCurrentUserProvider();
            var handler = new ValidatedPrincipalIdentityHandler(
                MockIdentityMapRepository(),
                MockProfileRepository(),
                currentUser,
                MockHttpContextAccessor(httpContext),
                MockWebHostEnvironment(),
                new DefaultLoggerFactory(),
                new LogValueSet());

            var testIdentity = testCase.TestIdentity;

            // upn claim is required for getting email
            if (!testIdentity.HasClaim((c) => c.Type == ClaimTypes.Upn))
            {
                var claim = testIdentity.FindFirst("upn")?.Value ?? testIdentity.FindFirst("preferred_username")?.Value;

                testIdentity.AddClaim(new Claim(ClaimTypes.Upn, claim));
            }

            var principal = new ClaimsPrincipal();
            principal.AddIdentity(testIdentity);

            var newPrincipal = await handler.ValidatedPrincipalAsync(principal, new JwtSecurityToken(DummyToken));

            Assert.IsType<VsoClaimsIdentity>(newPrincipal.Identity);
            var vsoIdentity = (VsoClaimsIdentity)newPrincipal.Identity;
            Assert.Equal(testCase.ExpectedHttpContextPlan, vsoIdentity.AuthorizedPlan);
            Assert.Equal(testCase.ExpectedHttpContextScopes, vsoIdentity.Scopes);
        }

        private static TestCase MockVsClaims()
        {
            var identity = new ClaimsIdentity();

            identity.AddClaim(new Claim("aud", Microsoft.VsSaaS.Common.Identity.AuthenticationConstants.VisualStudioClientAppId));
            identity.AddClaim(new Claim("appid", Microsoft.VsSaaS.Common.Identity.AuthenticationConstants.VisualStudioClientAppId));

            identity.AddClaim(new Claim("iss", "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/"));
            identity.AddClaim(new Claim("tid", "72f988bf-86f1-41af-91ab-2d7cd011db47"));

            // Random oid value
            identity.AddClaim(new Claim("oid", "965910b0-659d-43f4-a1b2-c0816a1534c9"));

            identity.AddClaim(new Claim("scp", "Directory.ReadWrite.All"));

            identity.AddClaim(new Claim("family_name", "User"));
            identity.AddClaim(new Claim("given_name", "Test"));
            identity.AddClaim(new Claim("name", "Test User"));
            identity.AddClaim(new Claim("upn", "test@email.com"));
            identity.AddClaim(new Claim("unique_name", "test@email.com"));

            identity.AddClaim(new Claim("ver", "1.0"));

            return new TestCase
            {
                TestIdentity = identity,
                ExpectedHttpContextPlan = null,
                ExpectedHttpContextScopes = null,
            };
        }

        private static TestCase MockAadClaims()
        {
            var identity = new ClaimsIdentity();

#pragma warning disable CS0618 // Type or member is obsolete
            identity.AddClaim(new Claim("aud", Microsoft.VsSaaS.Common.Identity.AuthenticationConstants.VisualStudioServicesDevApiAppId));
#pragma warning restore CS0618 // Type or member is obsolete

            identity.AddClaim(new Claim("iss", "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/v2.0"));
            identity.AddClaim(new Claim("tid", "72f988bf-86f1-41af-91ab-2d7cd011db47"));

            // Random oid value
            identity.AddClaim(new Claim("oid", "965910b0-659d-43f4-a1b2-c0816a1534c9"));

            identity.AddClaim(new Claim("scp", "All"));

            identity.AddClaim(new Claim("name", "Test User"));
            identity.AddClaim(new Claim("preferred_username", "test@email.com"));

            identity.AddClaim(new Claim("ver", "2.0"));

            return new TestCase
            {
                TestIdentity = identity,
                ExpectedHttpContextPlan = null,
                ExpectedHttpContextScopes = null,
            };
        }

        private static TestCase MockCascadeClaims(string planResourceId, IEnumerable<string> scopes)
        {
            var identity = new ClaimsIdentity();

            identity.AddClaim(new Claim("idp", "microsoft"));
            identity.AddClaim(new Claim("aud", "https://dev.liveshare.vsengsaas.visualstudio.com/"));
            identity.AddClaim(new Claim("iss", "https://dev.liveshare.vsengsaas.visualstudio.com/"));
            identity.AddClaim(new Claim("tid", "72f988bf-86f1-41af-91ab-2d7cd011db47"));

            // Random oid value
            identity.AddClaim(new Claim("oid", "965910b0-659d-43f4-a1b2-c0816a1534c9"));

            identity.AddClaim(new Claim("plan", planResourceId));
            identity.AddClaim(new Claim("scp", string.Join(" ", scopes)));

            identity.AddClaim(new Claim("name", "Test User"));
            identity.AddClaim(new Claim("email", "test@email.com"));
            identity.AddClaim(new Claim("preferred_username", "test@email.com"));

            identity.AddClaim(new Claim("ver", "2.0"));

            return new TestCase
            {
                TestIdentity = identity,
                ExpectedHttpContextPlan = planResourceId,
                ExpectedHttpContextScopes = scopes,
            };
        }

        private static TestCase MockDelegateCascadeClaims(string planResourceId, IEnumerable<string> scopes)
        {
            var identity = new ClaimsIdentity();

            identity.AddClaim(new Claim("idp", "vso"));
            identity.AddClaim(new Claim("aud", "https://dev.liveshare.vsengsaas.visualstudio.com/"));
            identity.AddClaim(new Claim("iss", "https://dev.liveshare.vsengsaas.visualstudio.com/"));

            // tid = planId (our Id, not the ResourceId), Oid = username
            identity.AddClaim(new Claim("tid", "6fe72daa-2c3a-470f-8a6e-d389098985f6"));
            identity.AddClaim(new Claim("oid", "test_user"));

            identity.AddClaim(new Claim("plan", planResourceId));
            identity.AddClaim(new Claim("scp", string.Join(" ", scopes)));

            identity.AddClaim(new Claim("name", "Test User"));
            identity.AddClaim(new Claim("preferred_username", "test_user"));

            identity.AddClaim(new Claim("ver", "2.0"));

            return new TestCase
            {
                TestIdentity = identity,
                ExpectedHttpContextPlan = planResourceId,
                ExpectedHttpContextScopes = scopes,
            };
        }

        private static IIdentityMapRepository MockIdentityMapRepository()
        {
            var entity = new Mock<IIdentityMapEntity>();

            var mock = new Mock<IIdentityMapRepository>();

            mock.Setup(obj => obj.GetByUserNameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(entity.Object);

            mock.Setup(obj => obj.BackgroundUpdateIfChangedAsync(It.IsAny<IIdentityMapEntity>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(entity.Object);

            return mock.Object;
        }

        private static IProfileRepository MockProfileRepository()
        {
            var mock = new Mock<IProfileRepository>();

            mock.Setup(obj => obj.GetCurrentUserProfileAsync(It.IsAny<IDiagnosticsLogger>()))
                .ReturnsAsync(new Profile());

            return mock.Object;
        }

        private static IWebHostEnvironment MockWebHostEnvironment()
        {
            var mock = new Mock<IWebHostEnvironment>();

            return mock.Object;
        }

        private static IHttpContextAccessor MockHttpContextAccessor(HttpContext context = null)
        {
            context ??= MockHttpContext.Create();

            var mock = new Mock<IHttpContextAccessor>();

            mock.SetupAllProperties();
            mock.Object.HttpContext = context;

            return mock.Object;
        }

        public class MockCurrentUserProvider : ICurrentUserProvider
        {
            public string BearerToken { get; set; }

            public string CanonicalUserId { get; set; }

            public UserIdSet CurrentUserIdSet { get; set; }

            public string IdMapKey { get; set; }

            public Profile Profile { get; set; }

            public ClaimsPrincipal Principal { get; set; }

            public VsoClaimsIdentity Identity
            {
                get { return (VsoClaimsIdentity)Principal.Identity; }
            }

            public void SetBearerToken(string token)
            {
                BearerToken = token;
            }

            public void SetPrincipal(ClaimsPrincipal principal)
            {
                Principal = principal;
            }

            public void SetProfile(Profile profile)
            {
                Profile = profile;
            }

            public void SetUserIds(string idMapKey, string canonicalUserId, string profileId, string profileProviderId)
            {
                CurrentUserIdSet = new UserIdSet(canonicalUserId, profileId, profileProviderId);
            }
        }
    }
}
