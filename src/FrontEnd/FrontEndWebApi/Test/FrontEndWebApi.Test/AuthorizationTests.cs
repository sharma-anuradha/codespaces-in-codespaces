using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Contracts;
using Xunit;
using Scopes = Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts.PlanAccessTokenScopes;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Test
{
    public class AuthorizationTests
    {
        private const string MockServiceUri = "https://testhost/test-service-uri/";

        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly IDiagnosticsLogger logger;

        public AuthorizationTests()
        {
            this.loggerFactory = new DefaultLoggerFactory();
            this.logger = loggerFactory.New();
        }

        public static TheoryData<AccessTest> ListData = new TheoryData<AccessTest>
        {
            AccessTest.Ok(scope: Scopes.ReadEnvironments, new[] { MockUtil.MockEnvironmentId }),
            AccessTest.Ok(scope: Scopes.WriteEnvironments, new[] { MockUtil.MockEnvironmentId }),
            AccessTest.Ok(scope: Scopes.ReadEnvironments, new[] { Guid.Empty.ToString(), MockUtil.MockEnvironmentId }),
            AccessTest.Ok(scope: Scopes.ReadEnvironments, new[] { MockUtil.MockEnvironmentId }, isMatchingPlan: true, isOwner: false),
            AccessTest.Ok(scope: Scopes.ReadEnvironments, new[] { Guid.Empty.ToString() }),
            AccessTest.Forbid(scope: Scopes.ReadEnvironments, new[] { MockUtil.MockEnvironmentId }, isMatchingPlan: false),
        };

        [Theory]
        [MemberData(nameof(ListData))]
        public async Task ListEnvironments(AccessTest test)
        {
            string userId = "test-user";
            string planId = "test-plan";

            var mockEnv = MockUtil.MockCloudEnvironment(userId, planId);
            var mockEnvManager = MockUtil.MockEnvironmentManager(mockEnv);

            var mockHttpContext = new DefaultHttpContext();
            
            var claimScopes = test.Scopes;
            var claimEnvironments = test.Environments;
            var claimPlanId = default(string);
            if (test.IsMatchingPlan == true)
            {
                claimPlanId = planId;
            }
            else if (test.IsMatchingPlan == false)
            {
                claimPlanId = "not-" + planId;
            }
            var mockIdentity = new VsoClaimsIdentity(claimPlanId, claimScopes, claimEnvironments, new ClaimsIdentity());

            var mockCurrentUserProvider = MockUtil.MockCurrentUserProvider(identity: mockIdentity);
            if (test.IsOwner)
            {
                mockEnv.OwnerId = mockCurrentUserProvider.CurrentUserIdSet?.PreferredUserId;
            }

            var environmentController = EnvironmentControllerTests.CreateTestEnvironmentsController(
                environmentManager: mockEnvManager,
                currentUserProvider: mockCurrentUserProvider,
                httpContext: mockHttpContext);

            var result = await environmentController.ListAsync(name: null, planId, logger);
            Assert.IsType(test.ExpectedResultType, result);

            if (test.ExpectedResultType == typeof(OkObjectResult))
            {
                var resultsArray = (CloudEnvironmentResult[])((OkObjectResult)result).Value;
                if (test.IsMatchingPlan != false &&
                    (test.Environments == null || test.Environments.Contains(MockUtil.MockEnvironmentId)))
                {
                    Assert.Single(resultsArray, (e) => e.Id == MockUtil.MockEnvironmentId);
                }
                else
                {
                    Assert.Empty(resultsArray);
                }
            }
        }

        public static TheoryData<AccessTest> GetData = new TheoryData<AccessTest>
        {
            AccessTest.Ok(scope: null, isMatchingPlan: null, isOwner: true),
            AccessTest.Ok(scope: null, isMatchingPlan: true, isOwner: true),
            AccessTest.Forbid(scope: null, isMatchingPlan: null, isOwner: false),
            AccessTest.Forbid(scope: null, isMatchingPlan: false, isOwner: false),
            AccessTest.Forbid(scope: null, isMatchingPlan: false, isOwner: true),
            AccessTest.Forbid(scope: null, isMatchingPlan: true, isOwner: false),

            AccessTest.Ok(scope: Scopes.ReadEnvironments, isMatchingPlan: true, isOwner: true),
            AccessTest.Ok(scope: Scopes.ReadEnvironments, isMatchingPlan: true, isOwner: false),
            AccessTest.Ok(scope: Scopes.ReadEnvironments, isMatchingPlan: null, isOwner: true),
            AccessTest.Ok(scope: Scopes.ReadEnvironments, isMatchingPlan: null, isOwner: false),
            AccessTest.Forbid(scope: Scopes.ReadEnvironments, isMatchingPlan: false, isOwner: true),
            AccessTest.Forbid(scope: Scopes.ReadEnvironments, isMatchingPlan: false, isOwner: false),

            AccessTest.Ok(scope: Scopes.WriteEnvironments, isMatchingPlan: null, isOwner: true),
            AccessTest.Ok(scope: Scopes.WriteEnvironments, isMatchingPlan: true, isOwner: true),
            AccessTest.Forbid(scope: Scopes.WriteEnvironments, isMatchingPlan: false, isOwner: true),
            AccessTest.Forbid(scope: Scopes.WriteEnvironments, isMatchingPlan: null, isOwner: false),
            AccessTest.Forbid(scope: Scopes.WriteEnvironments, isMatchingPlan: true, isOwner: false),
            AccessTest.Forbid(scope: Scopes.WriteEnvironments, isMatchingPlan: false, isOwner: false),

            AccessTest.Forbid(scope: Scopes.DeleteEnvironments, isMatchingPlan: true, isOwner: true),

            AccessTest.Ok(
                scopes: new[] { Scopes.ReadEnvironments, Scopes.DeleteEnvironments },
                isMatchingPlan: true,
                isOwner: false),

            AccessTest.Ok(scope: Scopes.ReadEnvironments, new[] { MockUtil.MockEnvironmentId }),
            AccessTest.Ok(scope: Scopes.WriteEnvironments, new[] { MockUtil.MockEnvironmentId }),
            AccessTest.Ok(scope: Scopes.ReadEnvironments, new[] { Guid.Empty.ToString(), MockUtil.MockEnvironmentId }),
            AccessTest.Ok(scope: Scopes.ReadEnvironments, new[] { MockUtil.MockEnvironmentId }, isMatchingPlan: true, isOwner: false),
            AccessTest.Forbid(scope: Scopes.ReadEnvironments, new[] { Guid.Empty.ToString() }),
            AccessTest.Forbid(scope: Scopes.ReadEnvironments, new[] { MockUtil.MockEnvironmentId }, isMatchingPlan: false),
        };

        [Theory]
        [MemberData(nameof(GetData))]
        public async Task GetEnvironment(AccessTest test)
        {
            string userId = "test-user";
            string planId = "test-plan";

            var mockEnv = MockUtil.MockCloudEnvironment(userId, planId);
            var mockEnvManager = MockUtil.MockEnvironmentManager(mockEnv);

            var mockHttpContext = new DefaultHttpContext();

            var claimScopes = test.Scopes;
            var claimEnvironments = test.Environments;
            var claimPlanId = default(string);
            if (test.IsMatchingPlan == true)
            {
                claimPlanId = planId;
            }
            else if (test.IsMatchingPlan == false)
            {
                claimPlanId = "not-" + planId;
            }
            var mockIdentity = new VsoClaimsIdentity(claimPlanId, claimScopes, claimEnvironments, new ClaimsIdentity());

            var mockCurrentUserProvider = MockUtil.MockCurrentUserProvider(identity: mockIdentity);
            if (test.IsOwner)
            {
                mockEnv.OwnerId = mockCurrentUserProvider.CurrentUserIdSet?.PreferredUserId;
            }

            var environmentController = EnvironmentControllerTests.CreateTestEnvironmentsController(
                environmentManager: mockEnvManager,
                currentUserProvider: mockCurrentUserProvider,
                httpContext: mockHttpContext);

            var result = await environmentController.GetAsync(mockEnv.Id, logger);
            Assert.IsType(test.ExpectedResultType, result);
        }

        public static TheoryData<AccessTest> CreateInOwnedPlanData = new TheoryData<AccessTest>
        {
            AccessTest.Created(scope: null, isMatchingPlan: null, isOwner: true),
            AccessTest.Created(scope: null, isMatchingPlan: true, isOwner: true),
            AccessTest.Forbid(scope: null, isMatchingPlan: null, isOwner: false),
            AccessTest.Forbid(scope: null, isMatchingPlan: false, isOwner: false),
            AccessTest.Forbid(scope: null, isMatchingPlan: false, isOwner: true),
            AccessTest.Forbid(scope: null, isMatchingPlan: true, isOwner: false),

            AccessTest.Forbid(scope: Scopes.ReadEnvironments, isMatchingPlan: true, isOwner: true),

            AccessTest.Created(scope: Scopes.WriteEnvironments, isMatchingPlan: null, isOwner: true),
            AccessTest.Created(scope: Scopes.WriteEnvironments, isMatchingPlan: null, isOwner: false),
            AccessTest.Created(scope: Scopes.WriteEnvironments, isMatchingPlan: true, isOwner: true),
            AccessTest.Created(scope: Scopes.WriteEnvironments, isMatchingPlan: true, isOwner: false),
            AccessTest.Forbid(scope: Scopes.WriteEnvironments, isMatchingPlan: false, isOwner: true),
            AccessTest.Forbid(scope: Scopes.WriteEnvironments, isMatchingPlan: false, isOwner: false),

            AccessTest.Forbid(scope: Scopes.DeleteEnvironments, isMatchingPlan: true, isOwner: true),

            AccessTest.Forbid(
                scopes: new[] { Scopes.ReadEnvironments, Scopes.DeleteEnvironments },
                isMatchingPlan: true,
                isOwner: false),
            AccessTest.Forbid(
                scopes: new[] { Scopes.ReadEnvironments, Scopes.DeleteEnvironments },
                isMatchingPlan: true,
                isOwner: true),
            AccessTest.Created(
                scopes: new[] { Scopes.ReadEnvironments, Scopes.WriteEnvironments, Scopes.DeleteEnvironments },
                isMatchingPlan: true,
                isOwner: false),
            AccessTest.Created(
                scopes: new[] { Scopes.ReadEnvironments, Scopes.WriteEnvironments, Scopes.DeleteEnvironments },
                isMatchingPlan: true,
                isOwner: true),

            AccessTest.Forbid(scope: Scopes.WriteEnvironments, new[] { MockUtil.MockEnvironmentId }, isMatchingPlan: true, isOwner: true),
        };

        public static TheoryData<AccessTest> CreateInSharedPlanData = new TheoryData<AccessTest>
        {
            AccessTest.Forbid(scope: null, isMatchingPlan: null),
            AccessTest.Forbid(scope: null, isMatchingPlan: false),
            AccessTest.Created(scope: null, isMatchingPlan: true),

            AccessTest.Forbid(scope: Scopes.ReadEnvironments, isMatchingPlan: true),

            AccessTest.Created(scope: Scopes.WriteEnvironments, isMatchingPlan: null),
            AccessTest.Created(scope: Scopes.WriteEnvironments, isMatchingPlan: true),
            AccessTest.Forbid(scope: Scopes.WriteEnvironments, isMatchingPlan: false),

            AccessTest.Forbid(scope: Scopes.DeleteEnvironments, isMatchingPlan: true),

            AccessTest.Forbid(
                scopes: new[] { Scopes.ReadEnvironments, Scopes.DeleteEnvironments },
                isMatchingPlan: true),
            AccessTest.Created(
                scopes: new[] { Scopes.ReadEnvironments, Scopes.WriteEnvironments, Scopes.DeleteEnvironments },
                isMatchingPlan: true),

            AccessTest.Forbid(scope: Scopes.WriteEnvironments, new[] { MockUtil.MockEnvironmentId }, isMatchingPlan: true, isOwner: false),
        };

        [Theory]
        [MemberData(nameof(CreateInOwnedPlanData))]
        public Task CreateEnvironmentInOwnedPlan(AccessTest test)
            => CreateEnvironment(test, ownedPlan: true);

        [Theory]
        [MemberData(nameof(CreateInSharedPlanData))]
        public Task CreateEnvironmentInSharedPlan(AccessTest test)
            => CreateEnvironment(test, ownedPlan: false);

        private async Task CreateEnvironment(AccessTest test, bool ownedPlan)
        {
            var body = new CreateCloudEnvironmentBody
            {
                FriendlyName = "test-environment",
                PlanId = (await MockUtil.GeneratePlan()).Plan.ResourceId,
                AutoShutdownDelayMinutes = 5,
                Type = EnvironmentType.CloudEnvironment.ToString(),
                SkuName = "testSkuName",
            };

            var uri = new Uri(MockServiceUri);
            var mockHttpContext = MockUtil.MockHttpContextFromUri(uri);
            var mockServiceUriBuilder = MockUtil.MockServiceUriBuilder(uri.ToString());

            var plan = await MockUtil.GeneratePlan(userId: test.IsOwner ?
                MockUtil.MockCurrentUserProvider().CurrentUserIdSet?.PreferredUserId : "other-user");
            if (!ownedPlan)
            {
                Assert.False(test.IsOwner);
                plan.UserId = null; // Create a "shared" plan that doesn't have a user ID.
            }

            var claimScopes = test.Scopes;
            var claimEnvironments = test.Environments;
            var claimPlanId = default(string);
            if (test.IsMatchingPlan == true)
            {
                claimPlanId = plan.Plan.ResourceId;
            }
            else if (test.IsMatchingPlan == false)
            {
                claimPlanId = "not-" + plan.Plan.ResourceId;
            }
            var mockIdentity = new VsoClaimsIdentity(claimPlanId, claimScopes, claimEnvironments, new ClaimsIdentity());

            var mockCurrentUserProvider = MockUtil.MockCurrentUserProvider(identity: mockIdentity);

            var mockPlanManager = MockUtil.MockPlanManager(() => Task.FromResult(plan));

            var environmentController = EnvironmentControllerTests.CreateTestEnvironmentsController(
                httpContext: mockHttpContext,
                planManager: mockPlanManager,
                serviceUriBuilder: mockServiceUriBuilder,
                currentUserProvider: mockCurrentUserProvider);

            var result = await environmentController.CreateAsync(body, logger);
            Assert.IsType(test.ExpectedResultType, result);
        }

        public static TheoryData<AccessTest> UpdateSettingsData = new TheoryData<AccessTest>
        {
            AccessTest.Ok(scope: Scopes.WriteEnvironments, new[] { MockUtil.MockEnvironmentId }),
            AccessTest.Ok(scope: Scopes.WriteEnvironments, new[] { Guid.Empty.ToString(), MockUtil.MockEnvironmentId }),
            AccessTest.Forbid(scope: Scopes.WriteEnvironments, new[] { Guid.Empty.ToString() }),
            AccessTest.Forbid(scope: Scopes.ReadEnvironments, new[] { MockUtil.MockEnvironmentId }),
            AccessTest.Forbid(scope: Scopes.WriteEnvironments, new[] { MockUtil.MockEnvironmentId }, isMatchingPlan: false),
            AccessTest.Forbid(scope: Scopes.WriteEnvironments, new[] { MockUtil.MockEnvironmentId }, isMatchingPlan: true, isOwner: false),
        };

        [Theory]
        [MemberData(nameof(UpdateSettingsData))]
        public async Task UpdateEnvironmentSettings(AccessTest test)
        {
            string userId = "test-user";
            string planId = "test-plan";

            var mockEnv = MockUtil.MockCloudEnvironment(userId, planId);
            var mockEnvManager = MockUtil.MockEnvironmentManager(mockEnv);

            var mockHttpContext = new DefaultHttpContext();

            var claimScopes = test.Scopes;
            var claimEnvironments = test.Environments;
            var claimPlanId = default(string);
            if (test.IsMatchingPlan == true)
            {
                claimPlanId = planId;
            }
            else if (test.IsMatchingPlan == false)
            {
                claimPlanId = "not-" + planId;
            }
            var mockIdentity = new VsoClaimsIdentity(claimPlanId, claimScopes, claimEnvironments, new ClaimsIdentity());

            var mockCurrentUserProvider = MockUtil.MockCurrentUserProvider(identity: mockIdentity);
            if (test.IsOwner)
            {
                mockEnv.OwnerId = mockCurrentUserProvider.CurrentUserIdSet?.PreferredUserId;
            }

            var environmentController = EnvironmentControllerTests.CreateTestEnvironmentsController(
                environmentManager: mockEnvManager,
                currentUserProvider: mockCurrentUserProvider,
                httpContext: mockHttpContext);

            var updateInput = new UpdateCloudEnvironmentBody();
            var result = await environmentController.UpdateSettingsAsync(
                mockEnv.Id, updateInput, logger);
            Assert.IsType(test.ExpectedResultType, result);
        }

        public class AccessTest
        {
            public static AccessTest Ok(string scope, bool? isMatchingPlan, bool isOwner = false)
                => Ok(scope == null ? null : new[] { scope }, isMatchingPlan, isOwner);
            public static AccessTest Ok(string[] scopes, bool? isMatchingPlan, bool isOwner = false)
                => new AccessTest
                {
                    Scopes = scopes,
                    IsMatchingPlan = isMatchingPlan,
                    IsOwner = isOwner,
                    ExpectedResultType = typeof(OkObjectResult),
                };
            public static AccessTest Ok(
                string scope, string[] environments, bool? isMatchingPlan = true, bool isOwner = true)
                => new AccessTest
                {
                    Scopes = new[] { scope },
                    Environments = environments,
                    IsMatchingPlan = isMatchingPlan,
                    IsOwner = isOwner,
                    ExpectedResultType = typeof(OkObjectResult),
                };

            public static AccessTest Created(string scope, bool? isMatchingPlan, bool isOwner = false)
                => Created(scope == null ? null : new[] { scope }, isMatchingPlan, isOwner);
            public static AccessTest Created(string[] scopes, bool? isMatchingPlan, bool isOwner = false)
                => new AccessTest
                {
                    Scopes = scopes,
                    IsMatchingPlan = isMatchingPlan,
                    IsOwner = isOwner,
                    ExpectedResultType = typeof(CreatedResult),
                };
            public static AccessTest Created(
                string scope, string[] environments, bool? isMatchingPlan = true, bool isOwner = true)
                => new AccessTest
                {
                    Scopes = new[] { scope },
                    Environments = environments,
                    IsMatchingPlan = isMatchingPlan,
                    IsOwner = isOwner,
                    ExpectedResultType = typeof(CreatedResult),
                };

            public static AccessTest Forbid(string scope, bool? isMatchingPlan, bool isOwner = false)
                => Forbid(scope == null ? null : new[] { scope }, isMatchingPlan, isOwner);
            public static AccessTest Forbid(string[] scopes, bool? isMatchingPlan, bool isOwner = false)
                => new AccessTest
                {
                    Scopes = scopes,
                    IsMatchingPlan = isMatchingPlan,
                    IsOwner = isOwner,
                    ExpectedResultType = typeof(ForbidResult),
                };
            public static AccessTest Forbid(
                string scope, string[] environments, bool? isMatchingPlan = true, bool isOwner = true)
                => new AccessTest
                {
                    Scopes = new[] { scope },
                    Environments = environments,
                    IsMatchingPlan = isMatchingPlan,
                    IsOwner = isOwner,
                    ExpectedResultType = typeof(ForbidResult),
                };

            public string[] Scopes { get; private set; }
            public string[] Environments { get; private set; }
            public bool? IsMatchingPlan { get; private set; }
            public bool IsOwner { get; private set; }
            public Type ExpectedResultType { get; private set; }
        }
    }
}
