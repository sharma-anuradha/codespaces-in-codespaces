using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Test
{
    public class BillingServiceTests : BaseBillingTests
    {
        private static readonly DateTime TestTimeNow = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0, DateTimeKind.Utc);
        private readonly BillingService billingService;
        private static readonly string WestUs2MeterId = "5f3afa79-01ad-4d7e-b691-73feca4ea350";


        // 5 hrs Available => 127 * 5 = 635
        private static readonly double BillableUnits = 635;
        // 3 hrs Available + 3hrs Shutdown => 127units * 3hrs + 2units * 2 hrs = 385
        private static readonly double BillableUnitsWithShutdown = 385;
        // (3 hrs Available + 1hr Shutdown on Standard) + (2 hrs Shutdown + 4 hrs Available on Premium) => (127units * 3hrs + 2units * 1hr) + (3units * 2hrs + 245units * 4hrs) = 1369
        private static readonly double BillableUnitsWithSkuChange = 1369;
        // 5 hr Available for 2 environments => (127units * 5)*2 = 1270
        private static readonly double BillableUnitsMultiEnvironments = 1270;
        // 5 hrs Available, 5 hrs Shutdown => (127units * 5) + (2units *5) = 645
        private static readonly double BIllableUnitsNoNewEvents = 645;

        // 5 hrs Shutdown => 2units * 5 = 10
        private static readonly double BillableUnitForShutdownEnvironment = 10;

        public static readonly IEnumerable<BillingEvent> BillingEventsInput = new List<BillingEvent>
        {
            new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Provisioning),
                    NewValue = nameof(CloudEnvironmentState.Available),
                },
                Environment = testEnvironment,
                Time = TestTimeNow.Subtract(TimeSpan.FromHours(6)),
                Type = BillingEventTypes.EnvironmentStateChange
            },
            new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Available),
                    NewValue = nameof(CloudEnvironmentState.Deleted),
                },
                Environment = testEnvironment,
                Time = TestTimeNow.Subtract(TimeSpan.FromHours(1)),
                Type = BillingEventTypes.EnvironmentStateChange

            },
        };
        public static readonly IEnumerable<BillingEvent> BillingEventsWithShutdownInput = new List<BillingEvent>
        {
            new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Created),
                    NewValue = nameof(CloudEnvironmentState.Available),
                },
                Environment = testEnvironment,
                Time = TestTimeNow.Subtract(TimeSpan.FromHours(6)),
                Type = BillingEventTypes.EnvironmentStateChange
            },
            new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Available),
                    NewValue = nameof(CloudEnvironmentState.Shutdown),
                },
                Environment = testEnvironment,
                Time = TestTimeNow.Subtract(TimeSpan.FromHours(3)),
                Type = BillingEventTypes.EnvironmentStateChange

            },
            new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Shutdown),
                    NewValue = nameof(CloudEnvironmentState.Deleted),
                },
                Environment = testEnvironment,
                Time =TestTimeNow.Subtract(TimeSpan.FromHours(1)),
                Type = BillingEventTypes.EnvironmentStateChange

            },
        };
        public static readonly IEnumerable<BillingEvent> BillingEventsWithSkuChange = new List<BillingEvent>
        {
            new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Created),
                    NewValue = nameof(CloudEnvironmentState.Available),
                },
                Environment = testEnvironment,
                Time = TestTimeNow.Subtract(TimeSpan.FromHours(11)),
                Type = BillingEventTypes.EnvironmentStateChange
            },
            new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Available),
                    NewValue = nameof(CloudEnvironmentState.Shutdown),
                },
                Environment = testEnvironment,
                Time = TestTimeNow.Subtract(TimeSpan.FromHours(8)),
                Type = BillingEventTypes.EnvironmentStateChange

            },
            new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Shutdown),
                    NewValue = nameof(CloudEnvironmentState.Shutdown),
                },
                Environment = testEnvironmentWithPremiumSku,
                Time = TestTimeNow.Subtract(TimeSpan.FromHours(7)),
                Type = BillingEventTypes.EnvironmentStateChange

            },
            new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Shutdown),
                    NewValue = nameof(CloudEnvironmentState.Available),
                },
                Environment = testEnvironmentWithPremiumSku,
                Time = TestTimeNow.Subtract(TimeSpan.FromHours(5)),
                Type = BillingEventTypes.EnvironmentStateChange

            },
            new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Available),
                    NewValue = nameof(CloudEnvironmentState.Deleted),
                },
                Environment = testEnvironmentWithPremiumSku,
                Time = TestTimeNow.Subtract(TimeSpan.FromHours(1)),
                Type = BillingEventTypes.EnvironmentStateChange
            },
        };
        public static readonly IEnumerable<BillingEvent> BillingEventsWithMultiEnvironmentsInput = new List<BillingEvent>
        {
            new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Created),
                    NewValue = nameof(CloudEnvironmentState.Available),
                },
                Environment = testEnvironment,
                Time = TestTimeNow.Subtract(TimeSpan.FromHours(6)),
                Type = BillingEventTypes.EnvironmentStateChange
            },
            new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Created),
                    NewValue = nameof(CloudEnvironmentState.Available),
                },
                Environment = testEnvironment2,
                Time = TestTimeNow.Subtract(TimeSpan.FromHours(6)),
                Type = BillingEventTypes.EnvironmentStateChange

            },
            new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Available),
                    NewValue = nameof(CloudEnvironmentState.Deleted),
                },
                Environment = testEnvironment,
                Time = TestTimeNow.Subtract(TimeSpan.FromHours(1)),
                Type = BillingEventTypes.EnvironmentStateChange

            },
            new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Available),
                    NewValue = nameof(CloudEnvironmentState.Deleted),
                },
                Environment = testEnvironment2,
                Time = TestTimeNow.Subtract(TimeSpan.FromHours(1)),
                Type = BillingEventTypes.EnvironmentStateChange

            },
        };


        public static readonly BillingSummary BillingSummaryInput = new BillingSummary
        {
            SubmissionState = BillingSubmissionState.None,
            Usage = new Dictionary<string, double>
            {
                { WestUs2MeterId, 0 },
            },
            UsageDetail = new UsageDetail
            {
                Environments = new Dictionary<string, EnvironmentUsageDetail>
                {
                    {
                        testEnvironment.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Available",
                            Sku = new Sku { Name = standardLinuxSkuName },
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, 0 },
                            },
                        }
                    },
                },

            },
            PeriodEnd = TestTimeNow.Subtract(TimeSpan.FromHours(6)),

        };
        // Same as BillingSummaryInput but with a longer Period
        public static readonly BillingSummary BillingSummaryInputForSkuChange = new BillingSummary
        {
            SubmissionState = BillingSubmissionState.None,
            Usage = new Dictionary<string, double>
            {
                { WestUs2MeterId, 0 },
            },
            UsageDetail = new UsageDetail
            {
                Environments = new Dictionary<string, EnvironmentUsageDetail>
                {
                    {
                        testEnvironment.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Available",
                            Sku = new Sku { Name = standardLinuxSkuName },
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, 0 },
                            },
                        }
                    },
                },
            },
            PeriodEnd = TestTimeNow.Subtract(TimeSpan.FromHours(11)),

        };
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
                            Sku = new Sku { Name = standardLinuxSkuName },
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, 0 },
                            },
                        }
                    },
                    {
                        testEnvironment2.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Shutdown",
                            Sku = new Sku { Name = standardLinuxSkuName },
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, 0 },
                            },
                        }
                    },
                    {
                        testEnvironment3.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Deleted",
                            Sku = new Sku { Name = standardLinuxSkuName },
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, 0 },
                            },
                        }
                    }
                },
            }
        };
        public static readonly BillingSummary BillingSummaryOutput = new BillingSummary
        {
            SubmissionState = BillingSubmissionState.None,
            Usage = new Dictionary<string, double>
            {
                {WestUs2MeterId, BillableUnits },
            },
            UsageDetail = new UsageDetail
            {
                Environments = new Dictionary<string, EnvironmentUsageDetail>
                {
                    {
                        testEnvironment.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Deleted",
                            Usage = new Dictionary<string, double>
                            {
                                {WestUs2MeterId, BillableUnits },
                            },
                        }
                    },
                },
            }
        };
        public static readonly BillingSummary BillingSummaryMultiOutput = new BillingSummary
        {
            Usage = new Dictionary<string, double>
            {
                {
                    WestUs2MeterId,
                    BillableUnitsMultiEnvironments
                },
            },
            UsageDetail = new UsageDetail
            {
                Environments = new Dictionary<string, EnvironmentUsageDetail>
                {
                    {
                        testEnvironment.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Deleted",
                            Usage = new Dictionary<string, double>
                            {
                                {WestUs2MeterId, BillableUnits },
                            },
                        }
                    },
                    {
                        testEnvironment2.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Deleted",
                            Usage = new Dictionary<string, double>
                            {
                                {WestUs2MeterId, BillableUnits },
                            },
                        }
                    },
                },
            }
        };
        public static readonly BillingSummary BillingSummaryWithShutdownOutput = new BillingSummary
        {
            SubmissionState = BillingSubmissionState.None,
            Usage = new Dictionary<string, double>
            {
                {WestUs2MeterId, BillableUnitsWithShutdown },
            },
            UsageDetail = new UsageDetail
            {
                Environments = new Dictionary<string, EnvironmentUsageDetail>
                {
                    {
                        testEnvironment.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Deleted",
                            Usage = new Dictionary<string, double>
                            {
                                {WestUs2MeterId, BillableUnitsWithShutdown },
                            },
                        }
                    },
                },
            }
        };
        public static readonly BillingSummary BillingSummaryWithSkuChangeOutput = new BillingSummary
        {
            SubmissionState = BillingSubmissionState.None,
            Usage = new Dictionary<string, double>
            {
                { WestUs2MeterId, BillableUnitsWithSkuChange },
            },
            UsageDetail = new UsageDetail
            {
                Environments = new Dictionary<string, EnvironmentUsageDetail>
                {
                    {
                        testEnvironment.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Deleted",
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, BillableUnitsWithSkuChange },
                            },
                        }
                    },
                },
            }
        };
        public static readonly BillingSummary BillingSummaryOutputNoCurrentEvents = new BillingSummary
        {
            SubmissionState = BillingSubmissionState.None,
            Usage = new Dictionary<string, double>
            {
                { WestUs2MeterId, BIllableUnitsNoNewEvents },
            },
            PeriodEnd = TestTimeNow.AddHours(-5),
            PeriodStart = TestTimeNow.AddHours(-4),
            UsageDetail = new UsageDetail
            {
                Environments = new Dictionary<string, EnvironmentUsageDetail>
                {
                    {
                        testEnvironment.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Available",
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, BillableUnits },
                            },
                        }
                    },
                    {
                        testEnvironment2.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Shutdown",
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, BillableUnitForShutdownEnvironment },
                            },
                        }
                    },
                },
            }
        };


        public BillingServiceTests()
        {
            var mockSkuCatelog = GetMockSKuCatalog();
            billingService = new BillingService(manager,
                                            new Mock<IControlPlaneInfo>().Object,
                                            mockSkuCatelog.Object,
                                            logger,
                                            new Mock<IClaimedDistributedLease>().Object,
                                            new MockTaskHelper(),
                                            planManager);
        }

        [Fact]
        public async Task CalculateBillingUnits_AvailableThenDeleted()
        {
            var inputEvents = BillingEventsInput;
            var inputSummary = BillingSummaryInput;
            var expectedSummary = BillingSummaryOutput;
            foreach (var input in inputEvents)
            {
                await repository.CreateAsync(input, logger);
            }
            //TODO: We should restructure these tests so that time can be an actual measurement that we calculate.
            var shardUsageTimes = new Dictionary<string, double>();
            // Billing Service
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes, logger);

            VerifySummary(expectedSummary, actualSummary);
        }

        [Fact]
        public async Task CalculateBillingUnits_SkuChangeInEvent()
        {
            var inputEvents = BillingEventsWithSkuChange;
            var inputSummary = BillingSummaryInputForSkuChange;
            var expectedSummary = BillingSummaryWithSkuChangeOutput;

            foreach (var input in inputEvents)
            {
                await repository.CreateAsync(input, logger);
            }

            Dictionary<string, double> shardUsageTimes = new Dictionary<string, double>();
            // Billing Service
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes, logger);

            VerifySummary(expectedSummary, actualSummary);
        }

        [Fact]
        public async Task CalculateBillingUnits_MultipleEnvironments()
        {
            var inputEvents = BillingEventsWithMultiEnvironmentsInput;
            var inputSummary = BillingSummaryInput;
            var expectedSummary = BillingSummaryMultiOutput;

            foreach (var input in inputEvents)
            {
                await repository.CreateAsync(input, logger);
            }
            //TODO: We should restructure these tests so that time can be an actual measurement that we calculate.
            Dictionary<string, double> shardUsageTimes = new Dictionary<string, double>();
            // Billing Service
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes, logger);

            VerifySummary(expectedSummary, actualSummary);
        }

        [Fact]
        public async Task CalculateBillingUnits_NoPreviousSummary()
        {
            var inputEvents = BillingEventsInput;
            var inputSummary = BillingSummaryInput;
            var expectedSummary = BillingSummaryOutput;

            BillingSummaryInput.UsageDetail = null;

            foreach (var input in inputEvents)
            {
                await repository.CreateAsync(input, logger);
            }
            //TODO: We should restructure these tests so that time can be an actual measurement that we calculate.
            Dictionary<string, double> shardUsageTimes = new Dictionary<string, double>();
            // Billing Service
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes, logger);

            VerifySummary(expectedSummary, actualSummary);
        }

        [Fact]
        public async Task BillingSummaryIsCreatedFromEvents_AvailableToShutdownToDeleted()
        {
            var inputEvents = BillingEventsWithShutdownInput;
            var inputSummary = BillingSummaryInput;
            var expectedSummary = BillingSummaryWithShutdownOutput;

            foreach (var input in inputEvents)
            {
                await repository.CreateAsync(input, logger);
            }
            //TODO: We should restructure these tests so that time can be an actual measurement that we calculate.
            Dictionary<string, double> shardUsageTimes = new Dictionary<string, double>();
            // Billing Service
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes, logger);

            VerifySummary(expectedSummary, actualSummary);
        }

        private static void VerifySummary(BillingSummary expectedSummary, BillingSummary actualSummary)
        {
            // Compare total billable units
            Assert.Equal(expectedSummary.Usage.First().Value, actualSummary.Usage.First().Value, 2);

            // MeterId match
            Assert.Equal(expectedSummary.Usage.First().Key, actualSummary.Usage.First().Key);

            var actualUsageDetail = actualSummary.UsageDetail;
            var expectedUsageDetail = expectedSummary.UsageDetail;

            // Environment list is not null and Count matches
            Assert.NotNull(actualUsageDetail.Environments);
            Assert.Equal(actualUsageDetail.Environments.Count(), expectedUsageDetail.Environments.Count());

            var actualEnvironmentUsageDetail = actualUsageDetail.Environments.First().Value;
            var expectedEnvironmentUsageDetail = expectedUsageDetail.Environments.First().Value;

            // Environment usage details 
            Assert.Equal(expectedEnvironmentUsageDetail.Usage.First().Value, actualEnvironmentUsageDetail.Usage.First().Value, 2);
            Assert.Equal(expectedEnvironmentUsageDetail.EndState, actualEnvironmentUsageDetail.EndState);
        }


        [Fact]
        public async Task CalculateBillingUnits_GlobalOverride_AvailableDeleted()
        {
            var inputEvents = BillingEventsInput;
            var inputSummary = BillingSummaryInput;

            foreach (var input in inputEvents)
            {
                await repository.CreateAsync(input, logger);
            }

            var billOverride = new BillingOverride()
            {
                Id = "test",
                StartTime = TestTimeNow.AddHours(-12),
                EndTime = TestTimeNow.AddHours(12),
                BillingOverrideState = BillingOverrideState.BillingDisabled,
                Priority = 1,
            };
            await overrideRepository.CreateAsync(billOverride, logger);

            var shardUsageTimes = new Dictionary<string, double>();
            // Billing Service
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes, logger);

            // Compare total billable units
            // Should be overriden with 0
            Assert.Equal(0, actualSummary.Usage.First().Value, 2);
        }

        [Fact]
        public async Task CalculateBillingUnits_GlobalOverride_AvailableShutdownDeleted()
        {
            var inputEvents = BillingEventsWithShutdownInput;
            var inputSummary = BillingSummaryInput;

            foreach (var input in inputEvents)
            {
                await repository.CreateAsync(input, logger);
            }

            var billOverride = new BillingOverride()
            {
                Id = "test",
                StartTime = TestTimeNow.AddHours(-12),
                EndTime = TestTimeNow.AddHours(12),
                BillingOverrideState = BillingOverrideState.BillingDisabled,
                Priority = 1,
            };
            await this.overrideRepository.CreateAsync(billOverride, logger);

            Dictionary<string, double> shardUsageTimes = new Dictionary<string, double>();
            // Billing Service
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes, logger);

            // Compare total billable units
            // Should be overriden with 0
            Assert.Equal(0, actualSummary.Usage.First().Value, 2);
        }

        [Fact]
        public async Task CalculateBillingUnits_GlobalOverride_SkuChange()
        {
            var inputEvents = BillingEventsWithSkuChange;
            var inputSummary = BillingSummaryInputForSkuChange;

            foreach (var input in inputEvents)
            {
                await repository.CreateAsync(input, logger);
            }

            var billOverride = new BillingOverride()
            {
                Id = "test",
                StartTime = TestTimeNow.AddHours(-12),
                EndTime = TestTimeNow.AddHours(12),
                BillingOverrideState = BillingOverrideState.BillingDisabled,
                Priority = 1,
            };
            await this.overrideRepository.CreateAsync(billOverride, logger);

            Dictionary<string, double> shardUsageTimes = new Dictionary<string, double>();
            // Billing Service
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes, logger);

            // Compare total billable units
            // Should be overriden with 0
            Assert.Equal(0, actualSummary.Usage.First().Value, 2);
        }

        [Fact]
        public async Task CalculateBillingUnits_GlobalOverride_MultiEnvironment()
        {
            var inputEvents = BillingEventsWithMultiEnvironmentsInput;
            var inputSummary = BillingSummaryInput;

            foreach (var input in inputEvents)
            {
                await repository.CreateAsync(input, logger);
            }

            var billOverride = new BillingOverride()
            {
                Id = "test",
                StartTime = TestTimeNow.AddHours(-12),
                EndTime = TestTimeNow.AddHours(12),
                BillingOverrideState = BillingOverrideState.BillingDisabled,
                Priority = 1,
            };
            await this.overrideRepository.CreateAsync(billOverride, logger);

            Dictionary<string, double> shardUsageTimes = new Dictionary<string, double>();
            // Billing Service
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes, logger);

            // Compare total billable units
            // Should be overriden with 0
            Assert.Equal(0, actualSummary.Usage.Values.Sum(), 2);
        }

        [Fact]
        public async Task CalculateBillingUnits_PlanOverride()
        {
            var inputEvents = BillingEventsInput;
            var inputSummary = BillingSummaryInput;
            var billingOverride = new BillingOverride()
            {
                Id = "testOverrideAccount",
                StartTime = TestTimeNow.AddHours(-7),
                EndTime = TestTimeNow.AddHours(12),
                BillingOverrideState = BillingOverrideState.BillingDisabled,
                Priority = 3,
                Plan = testPlan,
            };

            foreach (var input in inputEvents)
            {
                await repository.CreateAsync(input, logger);
            }
            await this.overrideRepository.CreateAsync(billingOverride, logger);
            Dictionary<string, double> shardUsageTimes = new Dictionary<string, double>();
            // Billing Service
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes, logger);

            // Compare total billable units
            // Should be overriden with 0
            Assert.Equal(0, actualSummary.Usage.First().Value, 2);
        }

        [Fact]
        public async Task CalculateBillingUnits_SkuOverride()
        {
            var inputEvents = BillingEventsInput;
            var inputSummary = BillingSummaryInput;
            var billingOverride = new BillingOverride()
            {
                Id = "testOverrideSKU",
                StartTime = TestTimeNow.AddHours(-7),
                EndTime = TestTimeNow.AddHours(12),
                BillingOverrideState = BillingOverrideState.BillingDisabled,
                Priority = 4,
                Sku = new Sku { Name = standardLinuxSkuName, Tier = "test" },
            };

            foreach (var input in inputEvents)
            {
                await repository.CreateAsync(input, logger);
            }
            await this.overrideRepository.CreateAsync(billingOverride, logger);
            Dictionary<string, double> shardUsageTimes = new Dictionary<string, double>();
            // Billing Service
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes, logger);

            // Compare total billable units
            // Should be overriden with 0
            Assert.Equal(0, actualSummary.Usage.First().Value, 2);
        }

        [Fact]
        public async Task CalculateBillingUnits_SubscriptionOverride()
        {
            var inputEvents = BillingEventsInput;
            var inputSummary = BillingSummaryInput;
            var billingOverride = new BillingOverride()
            {
                Id = "testOverrideSKU",
                StartTime = TestTimeNow.AddHours(-7),
                EndTime = TestTimeNow.AddHours(12),
                BillingOverrideState = BillingOverrideState.BillingDisabled,
                Priority = 4,
                Sku = new Sku { Name = standardLinuxSkuName, Tier = "test" },
            };

            foreach (var input in inputEvents)
            {
                await repository.CreateAsync(input, logger);
            }
            await overrideRepository.CreateAsync(billingOverride, logger);
            var shardUsageTimes = new Dictionary<string, double>();
            // Billing Service
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes, logger);

            // Compare total billable units
            // Should be overriden with 0
            Assert.Equal(0, actualSummary.Usage.First().Value, 2);
        }

        [Fact]
        public async Task CalculateBillingUnits_Partial()
        {
            var inputEvents = BillingEventsInput;
            var inputSummary = BillingSummaryInput;
            var billingOverride = new BillingOverride()
            {
                Id = "test",
                StartTime = TestTimeNow.AddHours(-4),
                EndTime = TestTimeNow.AddHours(-3),
                BillingOverrideState = BillingOverrideState.BillingDisabled,
                Priority = 1,
            };

            foreach (var input in inputEvents)
            {
                await repository.CreateAsync(input, logger);
            }
            await this.overrideRepository.CreateAsync(billingOverride, logger);
            Dictionary<string, double> shardUsageTimes = new Dictionary<string, double>();
            // Billing Service
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes, logger);

            Assert.Equal(508, actualSummary.Usage.First().Value, 2);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(48)]
        public async Task BillingSummaryIsCreatedNoNewEvents(int billDuration)
        {
            var billEventAvailable = new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Created),
                    NewValue = nameof(CloudEnvironmentState.Available),
                },
                Environment = testEnvironment,
                Time = TestTimeNow.AddHours(-(billDuration + 2)),
                Type = BillingEventTypes.EnvironmentStateChange
            };
            var billEventShutdown = new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Available),
                    NewValue = nameof(CloudEnvironmentState.Shutdown),
                },
                Environment = testEnvironment2,
                Time = TestTimeNow.AddHours(-(billDuration + 6)),
                Type = BillingEventTypes.EnvironmentStateChange

            };

            var billEventDeleted = new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Available),
                    NewValue = nameof(CloudEnvironmentState.Deleted),
                },
                Environment = testEnvironment3,
                Time = TestTimeNow.AddHours(-(billDuration + 6)),
                Type = BillingEventTypes.EnvironmentStateChange

            };

            await repository.CreateAsync(billEventAvailable, logger);
            await repository.CreateAsync(billEventShutdown, logger);
            await repository.CreateAsync(billEventDeleted, logger);

            double billableActiveHours = 127 * billDuration;
            double billableShutdownHours = 2 * billDuration;

            var latestBillingEvent = new BillingEvent
            {
                Args = new BillingSummary
                {
                    SubmissionState = BillingSubmissionState.None,
                    Usage = new Dictionary<string, double>
                    {
                        { WestUs2MeterId, 0 },
                    },
                    PeriodEnd = TestTimeNow.AddHours(-billDuration),
                    UsageDetail = new UsageDetail
                    {
                        Environments = new Dictionary<string, EnvironmentUsageDetail>
                        {
                            {
                                testEnvironment.Id,
                                new EnvironmentUsageDetail
                                {
                                    EndState = "Available",
                                    Sku = new Sku { Name = standardLinuxSkuName },
                                    Usage = new Dictionary<string, double>
                                    {
                                        { WestUs2MeterId, 0 },
                                    },
                                }
                            },
                            {
                                testEnvironment2.Id,
                                new EnvironmentUsageDetail
                                {
                                    EndState = "Shutdown",
                                    Sku = new Sku { Name = standardLinuxSkuName },
                                    Usage = new Dictionary<string, double>
                                    {
                                        { WestUs2MeterId, 0 },
                                    },
                                }
                            },
                            {
                                testEnvironment3.Id,
                                new EnvironmentUsageDetail
                                {
                                    EndState = "Deleted",
                                    Sku = new Sku { Name = standardLinuxSkuName },
                                    Usage = new Dictionary<string, double>
                                    {
                                        { WestUs2MeterId, 0 },
                                    },
                                }
                            }
                        },
                    }
                },
                // Last billing summary was written 3 hrs ago but should be irrelevant
                Time = TestTimeNow.Subtract(TimeSpan.FromHours(3)).AddMinutes(7),
            };
            var expectedSummary = new BillingSummary
            {
                SubmissionState = BillingSubmissionState.None,
                Usage = new Dictionary<string, double>
                {
                    { WestUs2MeterId, billableActiveHours + billableShutdownHours
                    },
                },
                PeriodEnd = TestTimeNow,
                PeriodStart = TestTimeNow.AddHours(-billDuration),
                UsageDetail = new UsageDetail
                {
                    Environments = new Dictionary<string, EnvironmentUsageDetail>
                {
                    {
                        testEnvironment.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Available",
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, billableActiveHours },
                            },
                        }
                    },
                    {
                        testEnvironment2.Id,
                        new EnvironmentUsageDetail
                        {
                            EndState = "Shutdown",
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, billableShutdownHours },
                            },
                        }
                    },
                },
                }
            };
            var shardUsageTimes = new Dictionary<string, double>();
            // BIlling Service
            var actualSummary = await billingService.CaculateBillingForEnvironmentsWithNoEvents(testPlan,
                                                                                        null,
                                                                                        latestBillingEvent,
                                                                                        TestTimeNow,
                                                                                        AzureLocation.WestUs2,
                                                                                        shardUsageTimes,
                                                                                        logger);

            // Compare total billable units
            Assert.Equal(expectedSummary.Usage.First().Value, actualSummary.Usage.First().Value, 2);

            // MeterId match
            Assert.Equal(expectedSummary.Usage.First().Key, actualSummary.Usage.First().Key);

            var actualUsageDetail = actualSummary.UsageDetail;
            var expectedUsageDetail = expectedSummary.UsageDetail;

            // Environment list is not null
            Assert.NotNull(actualUsageDetail.Environments);

            // Deleted Environment will not be in the newest billing summary
            Assert.Equal(2, actualUsageDetail.Environments.Count());
            Assert.DoesNotContain(testEnvironment3.Id, actualUsageDetail.Environments);

            var actualAvailableEnvironmentUsageDetail = actualUsageDetail.Environments.First().Value;
            var actualShutdownEnvironmentUsageDetail = actualUsageDetail.Environments.Last().Value;
            var expectedAvailableEnvironmentUsageDetail = expectedUsageDetail.Environments.First().Value;
            var expectedShutdownEnvironment2UsageDetail = expectedUsageDetail.Environments.Last().Value;

            // Environment billable usage
            Assert.Equal(expectedAvailableEnvironmentUsageDetail.Usage.First().Value, actualAvailableEnvironmentUsageDetail.Usage.First().Value, 2);
            Assert.Equal(expectedShutdownEnvironment2UsageDetail.Usage.First().Value, actualShutdownEnvironmentUsageDetail.Usage.First().Value, 2);

            // EndState should match because no new events are present
            Assert.Equal(expectedAvailableEnvironmentUsageDetail.EndState, actualAvailableEnvironmentUsageDetail.EndState);
            Assert.Equal(expectedShutdownEnvironment2UsageDetail.EndState, actualShutdownEnvironmentUsageDetail.EndState);
        }

        [Fact]
        public async Task GenerateBill_NoBillToGenerate()
        {
            var start = DateTime.UtcNow.AddHours(-4);
            var lastSummaryTime = start.AddHours(2);
            var endTime = start.AddHours(3);

            var plan = new VsoPlan()
            {
                Plan = new VsoPlanInfo()
                {
                    Name = "PlanName",
                    Location = AzureLocation.WestUs2,
                    ResourceGroup = "RG",
                    Subscription = Guid.NewGuid().ToString(),
                }
            };
            IEnumerable<VsoPlan> plans = new List<VsoPlan>() { plan };

            IEnumerable<BillingEvent> allBillingEvents = new List<BillingEvent>();
            IEnumerable<BillingEvent> oldEvents = new List<BillingEvent>();

            Mock<IControlPlaneInfo> controlPlane = new Mock<IControlPlaneInfo>();
            IEnumerable<AzureLocation> locations = new List<AzureLocation>() { AzureLocation.WestUs2 };
            Mock<IControlPlaneStampInfo> stampInfo = new Mock<IControlPlaneStampInfo>();
            Mock<ISkuCatalog> skuCatalog = new Mock<ISkuCatalog>();
            controlPlane.SetupGet(x => x.Stamp).Returns(stampInfo.Object);
            stampInfo.SetupGet(x => x.DataPlaneLocations).Returns(locations);
            Mock<ISkuCatalog> mockSkuCatelog = GetMockSKuCatalog();

            Mock<IBillingEventManager> billingEventManager = new Mock<IBillingEventManager>();
            Mock<IPlanManager> planManager = new Mock<IPlanManager>();
            Mock<IDiagnosticsLogger> logger = new Mock<IDiagnosticsLogger>();
            logger.Setup(x => x.WithValues(It.IsAny<LogValueSet>())).Returns(logger.Object);

            billingEventManager.Setup(x => x.GetPlanEventsAsync(It.IsAny<Expression<Func<BillingEvent, bool>>>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(oldEvents));
            billingEventManager.Setup(x => x.GetPlanEventsAsync(It.IsAny<VsoPlanInfo>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, logger.Object)).Returns(Task.FromResult(allBillingEvents));

            var shardTimes = new Dictionary<string, double>();

            // Setup a fake lease
            Mock<IClaimedDistributedLease> lease = new Mock<IClaimedDistributedLease>();
            BillingService sut = new BillingService(billingEventManager.Object, controlPlane.Object, mockSkuCatelog.Object, logger.Object, lease.Object, new MockTaskHelper(), planManager.Object);

            object argsInput = null;
            billingEventManager.Setup(x => x.CreateEventAsync(plan.Plan, null, BillingEventTypes.BillingSummary, It.IsAny<object>(), logger.Object)).Callback<VsoPlanInfo, EnvironmentBillingInfo, string, object, IDiagnosticsLogger>((p, env, type, args, l) => argsInput = args);
            await sut.BeginAccountCalculations(plan, start, endTime, logger.Object, AzureLocation.WestUs2, shardTimes);

            BillingSummary resultSummary = argsInput as BillingSummary;

            Assert.NotNull(resultSummary);
            Assert.Equal(0, resultSummary.Usage.Values.Sum());
        }

        [Fact]
        public async Task GenerateBill_BillZeroedOut()
        {
            var start = DateTime.UtcNow.AddHours(-4);
            var lastSummaryTime = start.AddHours(2);
            var endTime = start.AddHours(3);
            var billingOverride = new BillingOverride()
            {
                Id = "testOverrideAccount",
                StartTime = TestTimeNow.AddHours(-7),
                EndTime = TestTimeNow.AddHours(12),
                BillingOverrideState = BillingOverrideState.BillingDisabled,
                Priority = 3,
            };

            var billEventAvailable = new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Created),
                    NewValue = nameof(CloudEnvironmentState.Available),
                },
                Environment = testEnvironment,
                Time = start.AddHours(-20),
                Type = BillingEventTypes.EnvironmentStateChange
            };

            var plan = new VsoPlan()
            {
                Plan = new VsoPlanInfo()
                {
                    Name = "PlanName",
                    Location = AzureLocation.WestUs2,
                    ResourceGroup = "RG",
                    Subscription = Guid.NewGuid().ToString(),
                }
            };
            IEnumerable<VsoPlan> plans = new List<VsoPlan>() { plan };
            var lastBillingSummary = new BillingSummary()
            {
                PeriodEnd = lastSummaryTime,
                PeriodStart = DateTime.Now,
                SubmissionState = BillingSubmissionState.None,
                UsageDetail = new UsageDetail
                {
                    Environments = new Dictionary<string, EnvironmentUsageDetail>
                        {
                            {
                                testEnvironment.Id,
                                new EnvironmentUsageDetail
                                {
                                    EndState = "Available",
                                    Sku = new Sku { Name = standardLinuxSkuName },
                                    Usage = new Dictionary<string, double>
                                    {
                                        { WestUs2MeterId, 0 },
                                    },
                                }
                            },
                        },
                }
            };
            var lastSummaryEvent = new BillingEvent()
            {
                Plan = plan.Plan,
                Args = lastBillingSummary,
            };
            IEnumerable<BillingEvent> allBillingEvents = new List<BillingEvent>() { lastSummaryEvent };
            IEnumerable<BillingEvent> oldBillingEvents = new List<BillingEvent>() { billEventAvailable };

            Mock<IControlPlaneInfo> controlPlane = new Mock<IControlPlaneInfo>();
            IEnumerable<AzureLocation> locations = new List<AzureLocation>() { AzureLocation.WestUs2 };
            Mock<IControlPlaneStampInfo> stampInfo = new Mock<IControlPlaneStampInfo>();
            Mock<ISkuCatalog> skuCatalog = new Mock<ISkuCatalog>();
            controlPlane.SetupGet(x => x.Stamp).Returns(stampInfo.Object);
            stampInfo.SetupGet(x => x.DataPlaneLocations).Returns(locations);
            Mock<ISkuCatalog> mockSkuCatelog = GetMockSKuCatalog();

            Mock<IBillingEventManager> billingEventManager = new Mock<IBillingEventManager>();
            Mock<IPlanManager> planManager = new Mock<IPlanManager>();
            Mock<IDiagnosticsLogger> logger = new Mock<IDiagnosticsLogger>();
            logger.Setup(x => x.WithValues(It.IsAny<LogValueSet>())).Returns(logger.Object);
            billingEventManager.Setup(x => x.GetOverrideStateForTimeAsync(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<VsoPlanInfo>(), It.IsAny<Sku>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(billingOverride));
            billingEventManager.Setup(x => x.GetPlanEventsAsync(It.IsAny<Expression<Func<BillingEvent, bool>>>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(oldBillingEvents));
            billingEventManager.Setup(x => x.GetPlanEventsAsync(It.IsAny<VsoPlanInfo>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, logger.Object)).Returns(Task.FromResult(allBillingEvents));

            var shardTimes = new Dictionary<string, double>();

            // Setup a fake lease
            Mock<IClaimedDistributedLease> lease = new Mock<IClaimedDistributedLease>();
            BillingService sut = new BillingService(billingEventManager.Object, controlPlane.Object, mockSkuCatelog.Object, logger.Object, lease.Object, new MockTaskHelper(), planManager.Object);

            object argsInput = null;
            billingEventManager.Setup(x => x.CreateEventAsync(plan.Plan, null, BillingEventTypes.BillingSummary, It.IsAny<object>(), logger.Object)).Callback<VsoPlanInfo, EnvironmentBillingInfo, string, object, IDiagnosticsLogger>((p, env, type, args, l) => argsInput = args);
            await sut.BeginAccountCalculations(plan, start, endTime, logger.Object, AzureLocation.WestUs2, shardTimes);

            BillingSummary resultSummary = argsInput as BillingSummary;

            Assert.NotNull(resultSummary);

            Assert.Equal(1, resultSummary.Usage.Count);
            var usageTotal = resultSummary.Usage.Values.First();
            Assert.Equal(0, usageTotal, 4);

            Assert.Equal(BillingSubmissionState.None, resultSummary.SubmissionState);
        }

        [Fact]
        public async Task BeginAccountCalculations_NoStateChange()
        {
            var startTime = DateTime.UtcNow.AddHours(-4);
            var lastSummaryEndTime = startTime.AddHours(2);
            var endTime = startTime.AddHours(3);
            var expectedUsage = 127; // one hour of available time

            var billEventAvailable = new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Created),
                    NewValue = nameof(CloudEnvironmentState.Available),
                },
                Environment = testEnvironment,
                Time = startTime.AddHours(-20),
                Type = BillingEventTypes.EnvironmentStateChange
            };

            var plan = new VsoPlan()
            {
                Plan = new VsoPlanInfo()
                {
                    Name = "PlanName",
                    Location = AzureLocation.WestUs2,
                    ResourceGroup = "RG",
                    Subscription = Guid.NewGuid().ToString(),
                }
            };

            var lastBillingSummary = new BillingSummary()
            {
                PeriodEnd = lastSummaryEndTime,
                PeriodStart = DateTime.Now,
                SubmissionState = BillingSubmissionState.None,
                UsageDetail = new UsageDetail
                {
                    Environments = new Dictionary<string, EnvironmentUsageDetail>
                        {
                            {
                                testEnvironment.Id,
                                new EnvironmentUsageDetail
                                {
                                    EndState = "Available",
                                    Sku = new Sku { Name = standardLinuxSkuName },
                                    Usage = new Dictionary<string, double>
                                    {
                                        { WestUs2MeterId, 0 },
                                    },
                                }
                            },
                        },
                }
            };
            var lastSummaryEvent = new BillingEvent()
            {
                Plan = plan.Plan,
                Args = lastBillingSummary,
            };
            IEnumerable<BillingEvent> allBillingEvents = new List<BillingEvent>() { lastSummaryEvent };
            IEnumerable<BillingEvent> oldBillingEvents = new List<BillingEvent>() { billEventAvailable };

            await RunGenerateBillingSummaryTests(plan, allBillingEvents, oldBillingEvents, expectedUsage, startTime, lastSummaryEndTime, endTime);
        }

        [Fact]
        public async Task BeginAccountCalculations_StateChangeInMiddle()
        {
            var startTime = DateTime.UtcNow.AddHours(-4);
            var lastSummaryEndTime = startTime.AddHours(2);
            var endTime = startTime.AddHours(3);
            var expectedUsage = 127d / 2; // half hour of available time

            var billEventAvailable = new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Created),
                    NewValue = nameof(CloudEnvironmentState.Available),
                },
                Environment = testEnvironment,
                Time = lastSummaryEndTime.AddMinutes(30),
                Type = BillingEventTypes.EnvironmentStateChange
            };

            var plan = new VsoPlan()
            {
                Plan = new VsoPlanInfo()
                {
                    Name = "PlanName",
                    Location = AzureLocation.WestUs2,
                    ResourceGroup = "RG",
                    Subscription = Guid.NewGuid().ToString(),
                }
            };

            var lastBillingSummary = new BillingSummary()
            {
                PeriodEnd = lastSummaryEndTime,
                PeriodStart = lastSummaryEndTime.AddHours(-1),
            };
            var lastSummaryEvent = new BillingEvent()
            {
                Plan = plan.Plan,
                Args = lastBillingSummary,
            };
            IEnumerable<BillingEvent> allBillingEvents = new List<BillingEvent>() { lastSummaryEvent, billEventAvailable };
            IEnumerable<BillingEvent> oldBillingEvents = new List<BillingEvent>() { billEventAvailable };

            await RunGenerateBillingSummaryTests(plan, allBillingEvents, oldBillingEvents, expectedUsage, startTime, lastSummaryEndTime, endTime);
        }

        [Fact]
        public async Task BeginAccountCalculations_StateChangeInMiddleTheDeleted()
        {

            var startTime = DateTime.UtcNow.AddHours(-4);
            var lastSummaryEndTime = startTime.AddHours(2);
            var endTime = startTime.AddHours(3);
            var expectedUsage = 127d / 4; // 15 mins of available time

            var billEventAvailable = new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Created),
                    NewValue = nameof(CloudEnvironmentState.Available),
                },
                Environment = testEnvironment,
                Time = lastSummaryEndTime.AddMinutes(30),
                Type = BillingEventTypes.EnvironmentStateChange
            };

            var billEventDeleted = new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Available),
                    NewValue = nameof(CloudEnvironmentState.Deleted),
                },
                Environment = testEnvironment,
                Time = lastSummaryEndTime.AddMinutes(45),
                Type = BillingEventTypes.EnvironmentStateChange
            };

            var plan = new VsoPlan()
            {
                Plan = new VsoPlanInfo()
                {
                    Name = "PlanName",
                    Location = AzureLocation.WestUs2,
                    ResourceGroup = "RG",
                    Subscription = Guid.NewGuid().ToString(),
                }
            };

            var lastBillingSummary = new BillingSummary()
            {
                PeriodEnd = lastSummaryEndTime,
                PeriodStart = lastSummaryEndTime.AddHours(-1),
            };
            var lastSummaryEvent = new BillingEvent()
            {
                Plan = plan.Plan,
                Args = lastBillingSummary,
            };
            IEnumerable<BillingEvent> allBillingEvents = new List<BillingEvent>() { lastSummaryEvent, billEventAvailable, billEventDeleted };
            IEnumerable<BillingEvent> oldBillingEvents = new List<BillingEvent>() { billEventAvailable, billEventDeleted };

            await RunGenerateBillingSummaryTests(plan, allBillingEvents, oldBillingEvents, expectedUsage, startTime, lastSummaryEndTime, endTime);
        }

        [Fact]
        public async Task BeginAccountCalculations_DeletedBeforeTimeRange()
        {
            var startTime = DateTime.UtcNow.AddHours(-4);
            var lastSummaryEndTime = startTime.AddHours(2);
            var endTime = startTime.AddHours(3);
            double expectedUsage = 0; // Should have no usage. We were deleted prior to this billing period

            var billEventAvailable = new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Created),
                    NewValue = nameof(CloudEnvironmentState.Available),
                },
                Environment = testEnvironment,
                Time = lastSummaryEndTime.AddMinutes(-50),
                Type = BillingEventTypes.EnvironmentStateChange
            };

            var billEventDeleted = new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Plan = testPlan,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Available),
                    NewValue = nameof(CloudEnvironmentState.Deleted),
                },
                Environment = testEnvironment,
                Time = lastSummaryEndTime.AddMinutes(-45),
                Type = BillingEventTypes.EnvironmentStateChange
            };

            var plan = new VsoPlan()
            {
                Plan = new VsoPlanInfo()
                {
                    Name = "PlanName",
                    Location = AzureLocation.WestUs2,
                    ResourceGroup = "RG",
                    Subscription = Guid.NewGuid().ToString(),
                }
            };

            var lastBillingSummary = new BillingSummary()
            {
                PeriodEnd = lastSummaryEndTime,
                PeriodStart = lastSummaryEndTime.AddHours(-1),
            };
            var lastSummaryEvent = new BillingEvent()
            {
                Plan = plan.Plan,
                Args = lastBillingSummary,
            };
            IEnumerable<BillingEvent> allBillingEvents = new List<BillingEvent>() { lastSummaryEvent, billEventAvailable, billEventDeleted };
            IEnumerable<BillingEvent> oldBillingEvents = new List<BillingEvent>() { billEventAvailable, billEventDeleted };

            Mock<IControlPlaneInfo> controlPlane = new Mock<IControlPlaneInfo>();
            IEnumerable<AzureLocation> locations = new List<AzureLocation>() { AzureLocation.WestUs2 };
            Mock<IControlPlaneStampInfo> stampInfo = new Mock<IControlPlaneStampInfo>();
            Mock<ISkuCatalog> skuCatalog = new Mock<ISkuCatalog>();
            controlPlane.SetupGet(x => x.Stamp).Returns(stampInfo.Object);
            stampInfo.SetupGet(x => x.DataPlaneLocations).Returns(locations);
            Mock<ISkuCatalog> mockSkuCatelog = GetMockSKuCatalog();

            Mock<IBillingEventManager> billingEventManager = new Mock<IBillingEventManager>();
            Mock<IPlanManager> planManager = new Mock<IPlanManager>();
            Mock<IDiagnosticsLogger> logger = new Mock<IDiagnosticsLogger>();
            logger.Setup(x => x.WithValues(It.IsAny<LogValueSet>())).Returns(logger.Object);

            billingEventManager.Setup(x => x.GetPlanEventsAsync(It.IsAny<Expression<Func<BillingEvent, bool>>>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(oldBillingEvents));
            billingEventManager.Setup(x => x.GetPlanEventsAsync(It.IsAny<VsoPlanInfo>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, logger.Object)).Returns(Task.FromResult(allBillingEvents));

            var shardTimes = new Dictionary<string, double>();

            // Setup a fake lease
            Mock<IClaimedDistributedLease> lease = new Mock<IClaimedDistributedLease>();
            BillingService sut = new BillingService(billingEventManager.Object, controlPlane.Object, mockSkuCatelog.Object, logger.Object, lease.Object, new MockTaskHelper(), planManager.Object);

            object argsInput = null;
            billingEventManager.Setup(x => x.CreateEventAsync(plan.Plan, null, BillingEventTypes.BillingSummary, It.IsAny<object>(), logger.Object)).Callback<VsoPlanInfo, EnvironmentBillingInfo, string, object, IDiagnosticsLogger>((p, env, type, args, l) => argsInput = args);
            await sut.BeginAccountCalculations(plan, startTime, endTime, logger.Object, AzureLocation.WestUs2, shardTimes);

            BillingSummary resultSummary = argsInput as BillingSummary;

            Assert.NotNull(resultSummary);
            Assert.Equal(expectedUsage, resultSummary.Usage.Values.Sum());
        }

        private async Task RunGenerateBillingSummaryTests(VsoPlan plan, IEnumerable<BillingEvent> allBillingEvents, IEnumerable<BillingEvent> oldEvents, double expectedUsage, DateTime start, DateTime lastSummaryTime, DateTime endBillingTime)
        {
            var controlPlane = new Mock<IControlPlaneInfo>();
            IEnumerable<AzureLocation> locations = new List<AzureLocation>() { AzureLocation.WestUs2 };
            var stampInfo = new Mock<IControlPlaneStampInfo>();
            var skuCatalog = new Mock<ISkuCatalog>();
            controlPlane.SetupGet(x => x.Stamp).Returns(stampInfo.Object);
            stampInfo.SetupGet(x => x.DataPlaneLocations).Returns(locations);
            var mockSkuCatelog = GetMockSKuCatalog();

            var billingEventManager = new Mock<IBillingEventManager>();
            var planManager = new Mock<IPlanManager>();
            var logger = new Mock<IDiagnosticsLogger>();
            logger.Setup(x => x.WithValues(It.IsAny<LogValueSet>())).Returns(logger.Object);

            billingEventManager.Setup(x => x.GetPlanEventsAsync(It.IsAny<Expression<Func<BillingEvent, bool>>>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(oldEvents));
            billingEventManager.Setup(x => x.GetPlanEventsAsync(It.IsAny<VsoPlanInfo>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, logger.Object)).Returns(Task.FromResult(allBillingEvents));

            var shardTimes = new Dictionary<string, double>();

            // Setup a fake lease
            var lease = new Mock<IClaimedDistributedLease>();
            var sut = new BillingService(billingEventManager.Object, controlPlane.Object, mockSkuCatelog.Object, logger.Object, lease.Object, new MockTaskHelper(), planManager.Object);

            object argsInput = null;
            billingEventManager.Setup(x => x.CreateEventAsync(plan.Plan, null, BillingEventTypes.BillingSummary, It.IsAny<object>(), logger.Object)).Callback<VsoPlanInfo, EnvironmentBillingInfo, string, object, IDiagnosticsLogger>((p, env, type, args, l) => argsInput = args);
            await sut.BeginAccountCalculations(plan, start, endBillingTime, logger.Object, AzureLocation.WestUs2, shardTimes);

            var resultSummary = argsInput as BillingSummary;

            Assert.NotNull(resultSummary);
            Assert.Equal(endBillingTime, resultSummary.PeriodEnd);
            Assert.Equal(lastSummaryTime, resultSummary.PeriodStart);

            Assert.Equal(1, resultSummary.Usage.Count);
            var usageTotal = resultSummary.Usage.Values.First();
            Assert.Equal(expectedUsage, usageTotal, 4);
            Assert.Equal(BillingSubmissionState.None, resultSummary.SubmissionState);
        }
    }
}
