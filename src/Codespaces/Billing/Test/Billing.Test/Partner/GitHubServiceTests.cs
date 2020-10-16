using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Test
{
    public class GitHubServiceTests
    {
        private static readonly DateTime TestTimeNow = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0, DateTimeKind.Utc);
        private const string Gh = "gh";
        private const string GitHub = "github";
        private const string GitHubSubscription = "d833c9b9-c971-47f1-8156-c4236552bdfd";

        public enum Missing
        {
            None,
            UsageDetail,
            Environments,
            ResourceUsage,
            Lists
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(0, 1)]
        [InlineData(0, 0)]
        [InlineData(0, 0, Missing.UsageDetail)]
        [InlineData(0, 0, Missing.Environments)]
        [InlineData(0, 0, Missing.ResourceUsage)]
        [InlineData(0, 0, Missing.Lists)]
        public async void SubmitPartnerInvoice(
            int compute,
            int storage,
            Missing missing = default)
        {
            var controlPlane = new Mock<IControlPlaneInfo>();
            var stampInfo = new Mock<IControlPlaneStampInfo>();
            var planManager = new Mock<IPlanManager>();
            var logger = new Mock<IDiagnosticsLogger>();
            var billingEventManager = new Mock<IBillingEventManager>();
            var factory = new Mock<IPartnerCloudStorageFactory>();
            var client = new Mock<IPartnerCloudStorageClient>();
            var lease = new Mock<IClaimedDistributedLease>();
            var taskHelper = new MockTaskHelper();

            controlPlane
                .SetupGet(x => x.Stamp)
                .Returns(stampInfo.Object);

            stampInfo
                .SetupGet(x => x.DataPlaneLocations)
                .Returns(new[] { AzureLocation.WestUs2 });

            logger
                .Setup(x => x.WithValues(It.IsAny<LogValueSet>()))
                .Returns(logger.Object);

            var planInfo = new VsoPlanInfo()
            {
                Name = "PlanName",
                Location = AzureLocation.WestUs2,
                ResourceGroup = "RG",
                Subscription = GitHubSubscription,
            };

            var plan = new VsoPlan()
            {
                Plan = planInfo,
                Partner = Partner.GitHub
            };

            var usage = new Dictionary<string, double>
            {
                { "meter", 3d }
            };

            var resourceUsage = new ResourceUsageDetail()
            {
                Compute = missing == Missing.Lists ? null : new List<ComputeUsageDetail>() { new ComputeUsageDetail() { Usage = compute } },
                Storage = missing == Missing.Lists ? null : new List<StorageUsageDetail>() { new StorageUsageDetail() { Usage = storage } }
            };

            var environmentUsageDetail = new EnvironmentUsageDetail()
            {
                Name = "FooBar",
                UserId = Guid.NewGuid().ToString(),
                ResourceUsage = missing == Missing.ResourceUsage ? null : resourceUsage,
            };

            var usageDetail = new UsageDetail()
            {
                Environments = missing == Missing.Environments ? null :
                    new Dictionary<string, EnvironmentUsageDetail>()
                    {
                        {  Guid.NewGuid().ToString(), environmentUsageDetail },
                    }
            };

            var billingSummary = new BillingSummary()
            {
                PeriodEnd = DateTime.Now,
                PeriodStart = DateTime.Now,
                Plan = "test",
                SubmissionState = BillingSubmissionState.None,
                Usage = usage,
                UsageDetail = missing == Missing.UsageDetail ? null : usageDetail,
            };

            var billingEvent = new BillingEvent()
            {
                Plan = planInfo,
                Args = billingSummary,
            };

            var partnerQueueSubmission = new PartnerQueueSubmission(billingEvent);

            var isEmptySubmission = (missing != default) || (compute == 0 && storage == 0);
            Assert.True(partnerQueueSubmission.IsEmpty() == isEmptySubmission);

            planManager.Setup(x =>
                x.GetPartnerPlansByShardAsync(
                    It.IsAny<IEnumerable<AzureLocation>>(),
                    It.IsAny<string>(),
                    It.IsAny<Partner>(),
                    It.IsAny<IDiagnosticsLogger>())
                ).Returns(Task.FromResult(new[] { plan } as IEnumerable<VsoPlan>)
            );

            billingEventManager.Setup(x =>
                x.GetPlanEventsAsync(
                    It.IsAny<Expression<Func<BillingEvent, bool>>>(), logger.Object)
                ).Returns(Task.FromResult(new[] { billingEvent } as IEnumerable<BillingEvent>)
            );

            planManager.Setup(x =>
                x.GetShards()
            ).Returns(new List<string>() { GitHubSubscription[0].ToString() });

            factory.Setup(x => x.CreatePartnerCloudStorage(
                It.IsAny<AzureLocation>(), Gh)
            ).Returns(Task.FromResult(client.Object));

            client.Setup(x =>
                x.PushPartnerQueueSubmission(
                    It.IsAny<PartnerQueueSubmission>()
                )
            ).Returns(Task.FromResult(partnerQueueSubmission));

            // Setup a fake lease
            lease.Setup(x =>
                x.Obtain(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<IDiagnosticsLogger>()
                )
            ).Returns(Task.FromResult(new FakeLease() as IDisposable));

            var sut = new GitHubService(
                controlPlane.Object,
                billingEventManager.Object,
                logger.Object,
                factory.Object,
                lease.Object,
                taskHelper,
                planManager.Object
            );

            await sut.Execute(new System.Threading.CancellationToken());

            var expectedBillingSubmissionState = isEmptySubmission ?
                BillingSubmissionState.NeverSubmit : BillingSubmissionState.Submitted;
            Assert.Equal(expectedBillingSubmissionState, billingSummary.PartnerSubmissionState);
        }


        [Fact]
        public void Serialize_and_validate_PartnerSubmission()
        {
            // Note: This test governs the partner interaction between our billing system and github system.
            // ANY changes to this test must be be correlated with GitHub otherwise billing systems will break down as this test confirms the schema we are sending to GitHub.

            var planInfo = new VsoPlanInfo()
            {
                Name = "PlanName",
                Location = AzureLocation.WestUs2,
                ResourceGroup = "RG",
                Subscription = GitHubSubscription,
            };

            var billEvent = new BillingEvent
            {
                Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                Plan = planInfo,
                Args = BillingSummaryInputNoCurrentEvents,
                Time = TestTimeNow.Subtract(TimeSpan.FromHours(6)),
                Type = BillingEventTypes.BillingSummary
            };

            var queueMessage = new PartnerQueueSubmission(billEvent);

            ValidatePartnerQueueSubmission(queueMessage);
        }

        [Fact]
        public void Serialize_and_validate_PartnerSubmissionBillingV2()
        {
            var planInfo = new VsoPlanInfo()
            {
                Name = "PlanName",
                Location = AzureLocation.WestUs2,
                ResourceGroup = "RG",
                Subscription = GitHubSubscription,
            };

            var billSummary = new BillSummary()
            {
                Plan = planInfo,
                BillGenerationTime = TestTimeNow.Subtract(TimeSpan.FromHours(6)),
                Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                PeriodEnd = TestTimeNow.AddHours(-4),
                UsageDetail = new List<EnvironmentUsage>()
                {
                    new EnvironmentUsage()
                    {
                        Id = testEnvironment.Id,
                         EndState = "Available",
                            Sku = new Sku { Name = standardLinuxSkuName },
                        ResourceUsage = new ResourceUsageDetail
                            {
                                Compute = new List<ComputeUsageDetail>{ new ComputeUsageDetail
                                    {
                                        Sku = standardLinuxSkuName,
                                        Usage = 3600,
                                    }
                                },
                                Storage = new List<StorageUsageDetail>
                                {
                                    new StorageUsageDetail
                                    {
                                        Sku = standardLinuxSkuName,
                                        Usage = 3600,
                                        Size = 64,
                                    }
                                }
                            },
                    },
                    new EnvironmentUsage()
                    {
                          Id = testEnvironment2.Id,

                            EndState = "Shutdown",
                            Sku = new Sku { Name = standardLinuxSkuName },
                             ResourceUsage = new ResourceUsageDetail
                            {
                                Storage = new List<StorageUsageDetail>
                                {
                                    new StorageUsageDetail
                                    {
                                        Sku = standardLinuxSkuName,
                                        Usage = 3600,
                                        Size = 64,
                                    }
                                }
                            }
                    },
                    new EnvironmentUsage()
                    {
                          Id = testEnvironment3.Id,

                            EndState = "Deleted",
                            Sku = new Sku { Name = standardLinuxSkuName },
                             ResourceUsage = new ResourceUsageDetail
                            {
                                Storage = new List<StorageUsageDetail>
                                {
                                    new StorageUsageDetail
                                    {
                                        Sku = standardLinuxSkuName,
                                        Usage = 1800,
                                        Size = 64,
                                    }
                                }
                            }
                    }
                },
            };

            var queueMessage = new PartnerQueueSubmission(billSummary);

            ValidatePartnerQueueSubmission(queueMessage);
        }

        private static void ValidatePartnerQueueSubmission(PartnerQueueSubmission queueMessage)
        {
            var jsonBlob = queueMessage.ToJson();
            var jObject = JObject.Parse(jsonBlob);

            var subSchema = GetGitHubBillingSchema();

            // validate against the JSON schema
            Assert.True(jObject.IsValid(subSchema, out IList<string> errorMessages), string.Join(Environment.NewLine, errorMessages));
        }

        private static JSchema GetGitHubBillingSchema()
        {
            var schemaReader = File.OpenText(Path.Combine("Partner", "vscs_billing_data_schema.json"));
            var schema = JSchema.Load(new JsonTextReader(schemaReader));
            JSchema subSchema = schema.Properties["additionalPropertiesDenied"];
            return subSchema;
        }

        public static readonly string standardLinuxSkuName = "standardLinuxSku";
        public static readonly EnvironmentBillingInfo testEnvironment = new EnvironmentBillingInfo
        {
            Id = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            Name = "testEnvironment",
            Sku = new Sku { Name = standardLinuxSkuName, Tier = "test" },
        };
        public static readonly EnvironmentBillingInfo testEnvironment2 = new EnvironmentBillingInfo
        {
            Id = "cccccccc-cccc-cccc-cccc-cccccccccccc",
            Name = "testEnvironment2",
            Sku = new Sku { Name = standardLinuxSkuName, Tier = "test" },
        };
        public static readonly EnvironmentBillingInfo testEnvironment3 = new EnvironmentBillingInfo
        {
            Id = "dddddddd-dddd-dddd-dddd-ddddddddddd",
            Name = "testEnvironment3",
            Sku = new Sku { Name = standardLinuxSkuName, Tier = "test" },
        };
        private static readonly string WestUs2MeterId = "5f3afa79-01ad-4d7e-b691-73feca4ea350";



        public static readonly BillingSummary BillingSummaryInputNoCurrentEvents = new BillingSummary
        {
            SubmissionState = BillingSubmissionState.None,
            Usage = new Dictionary<string, double>
            {
                { WestUs2MeterId, 0 },
            },
            PeriodEnd = TestTimeNow.AddHours(-4),
            UsageDetail = new UsageDetail
            {
                Environments = new Dictionary<string, EnvironmentUsageDetail>
                {
                    {
                        testEnvironment.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Available",
                            Name = testEnvironment.Name,
                            Sku = new Sku { Name = standardLinuxSkuName },
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, 0 },
                            },
                            ResourceUsage = new ResourceUsageDetail
                            {
                                Compute = new List<ComputeUsageDetail>{ new ComputeUsageDetail
                                    {
                                        Sku = standardLinuxSkuName,
                                        Usage = 3600,
                                    }
                                },
                                Storage = new List<StorageUsageDetail>
                                {
                                    new StorageUsageDetail
                                    {
                                        Sku = standardLinuxSkuName,
                                        Usage = 3600,
                                        Size = 64,
                                    }
                                }
                            }
                        }
                    },
                    {
                        testEnvironment2.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Shutdown",
                            Name = testEnvironment2.Name,
                            Sku = new Sku { Name = standardLinuxSkuName },
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, 0 },
                            },
                             ResourceUsage = new ResourceUsageDetail
                            {
                                Storage = new List<StorageUsageDetail>
                                {
                                    new StorageUsageDetail
                                    {
                                        Sku = standardLinuxSkuName,
                                        Usage = 3600,
                                        Size = 64,
                                    }
                                }
                            }
                        }
                    },
                    {
                        testEnvironment3.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Deleted",
                            Name = testEnvironment3.Name,
                            Sku = new Sku { Name = standardLinuxSkuName },
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, 0 },
                            },
                             ResourceUsage = new ResourceUsageDetail
                            {
                                Storage = new List<StorageUsageDetail>
                                {
                                    new StorageUsageDetail
                                    {
                                        Sku = standardLinuxSkuName,
                                        Usage = 1800,
                                        Size = 64,
                                    }
                                }
                            }
                        }
                    }
                },
            }
        };

    }
}
