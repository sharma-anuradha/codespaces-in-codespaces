using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
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
        private readonly decimal standardLinuxComputeUnitPerHr = 125;
        private readonly decimal premiumLinuxComputeUnitPerHr = 242;
        private readonly decimal standardLinuxStorageUnitPerHr = 2;
        private readonly decimal premiumLinuxStorageUnitPerHr = 3;
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

        public static readonly BillingOverride BillingOverrideGlobal = new BillingOverride()
        {
            Id = "test",
            StartTime = TestTimeNow.AddHours(-7),
            EndTime = TestTimeNow.AddHours(12),
            BillingOverrideState = BillingOverrideState.BillingDisabled,
            Priority = 1,
        };

        public static readonly BillingOverride BillingOverrideGlobalSmall = new BillingOverride()
        {
            Id = "test",
            StartTime = TestTimeNow.AddHours(-4),
            EndTime = TestTimeNow.AddHours(-3),
            BillingOverrideState = BillingOverrideState.BillingDisabled,
            Priority = 1,
        };

        public static readonly BillingOverride BillingOverrideSubscription = new BillingOverride()
        {
            Id = "testOverrideSub",
            StartTime = TestTimeNow.AddHours(-7),
            EndTime = TestTimeNow.AddHours(12),
            BillingOverrideState = BillingOverrideState.BillingDisabled,
            Priority = 2,
            Subscription = testPlan.Subscription,
        };

        public static readonly BillingOverride BillingOverrideAccount = new BillingOverride()
        {
            Id = "testOverrideAccount",
            StartTime = TestTimeNow.AddHours(-7),
            EndTime = TestTimeNow.AddHours(12),
            BillingOverrideState = BillingOverrideState.BillingDisabled,
            Priority = 3,
            Plan = testPlan,
        };

        public static readonly BillingOverride BillingOverrideSKU = new BillingOverride()
        {
            Id = "testOverrideSKU",
            StartTime = TestTimeNow.AddHours(-7),
            EndTime = TestTimeNow.AddHours(12),
            BillingOverrideState = BillingOverrideState.BillingDisabled,
            Priority = 4,
            Sku = new Sku { Name = standardLinuxSkuName, Tier = "test" },
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
                            UserId = testEnvironment.UserId,
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, 0 },
                            },
                        }
                    },
                },
                Users = new Dictionary<string, UserUsageDetail>
                {
                    {
                        testEnvironment.UserId,
                        new UserUsageDetail
                        {
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, 0 },
                            },

                        }
                    }
                }

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
                            UserId = testEnvironment.UserId,
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, 0 },
                            },
                        }
                    },
                },
                Users = new Dictionary<string, UserUsageDetail>
                {
                    {
                        testEnvironment.UserId,
                        new UserUsageDetail
                        {
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, 0 },
                            },

                        }
                    }
                }

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
                            UserId = testEnvironment.UserId,
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
                            UserId = testEnvironment.UserId,
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
                            UserId = testEnvironment.UserId,
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, 0 },
                            },
                        }
                    }
                },
                Users = new Dictionary<string, UserUsageDetail>
                {
                    {
                        testEnvironment.UserId,
                        new UserUsageDetail
                        {
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, 0 },
                            },

                        }
                    }
                }

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
                            UserId = testEnvironment.UserId,
                            Usage = new Dictionary<string, double>
                            {
                                {WestUs2MeterId, BillableUnits },
                            },
                        }
                    },
                },
                Users = new Dictionary<string, UserUsageDetail>
                {
                    {
                        testEnvironment.UserId,
                        new UserUsageDetail
                        {
                            Usage = new Dictionary<string, double>
                            {
                                {WestUs2MeterId, BillableUnits },
                            },

                        }
                    }
                }

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
                            UserId = testEnvironment.UserId,
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
                            UserId = testEnvironment2.UserId,
                            Usage = new Dictionary<string, double>
                            {
                                {WestUs2MeterId, BillableUnits },
                            },
                        }
                    },
                },
                Users = new Dictionary<string, UserUsageDetail>
                {
                    {
                        testEnvironment.UserId,
                        new UserUsageDetail
                        {
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, BillableUnitsMultiEnvironments },
                            },

                        }
                    },
                }

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
                            UserId = testEnvironment.UserId,
                            Usage = new Dictionary<string, double>
                            {
                                {WestUs2MeterId, BillableUnitsWithShutdown },
                            },
                        }
                    },
                },
                Users = new Dictionary<string, UserUsageDetail>
                {
                    {
                        testEnvironment.UserId,
                        new UserUsageDetail
                        {
                            Usage = new Dictionary<string, double>
                            {
                                {WestUs2MeterId, BillableUnitsWithShutdown },
                            },

                        }
                    }
                }

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
                            UserId = testEnvironment.UserId,
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, BillableUnitsWithSkuChange },
                            },
                        }
                    },
                },
                Users = new Dictionary<string, UserUsageDetail>
                {
                    {
                        testEnvironment.UserId,
                        new UserUsageDetail
                        {
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, BillableUnitsWithSkuChange },
                            },

                        }
                    }
                }

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
                            UserId = testEnvironment.UserId,
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
                            UserId = testEnvironment.UserId,
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, BillableUnitForShutdownEnvironment },
                            },
                        }
                    },
                },
                Users = new Dictionary<string, UserUsageDetail>
                {
                    {
                        testEnvironment.UserId,
                        new UserUsageDetail
                        {
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, BIllableUnitsNoNewEvents },
                            },

                        }
                    }
                }

            }
        };

        /// <summary>
        /// Gets the input parameters for the billing calculations
        /// Each yield statement represent a seperate tests run
        /// </summary>
        /// <returns>An object array representing all 3 inputs to the test method.</returns>
        public static IEnumerable<object[]> GetBillingInputsWithNewEvents()
        {
            // Test billing calculations with Available and Deleted events.
            yield return new object[] { BillingEventsInput, BillingSummaryInput, BillingSummaryOutput };

            // Test billing calculation with Available, Shutdown, and Deleted events.
            yield return new object[] { BillingEventsWithShutdownInput, BillingSummaryInput, BillingSummaryWithShutdownOutput };

            // Test billing calculation with SKU changes
            yield return new object[] { BillingEventsWithSkuChange, BillingSummaryInputForSkuChange, BillingSummaryWithSkuChangeOutput };

            // Test billing calculations with Available and Shutdown events on 2 environments.
            yield return new object[] { BillingEventsWithMultiEnvironmentsInput, BillingSummaryInput, BillingSummaryMultiOutput };

            // Test billing calculations with Available and Deleted events and No previous billing summary.
            BillingSummaryInput.UsageDetail = null;
            yield return new object[] { BillingEventsInput, BillingSummaryInput, BillingSummaryOutput };
        }

        /// <summary>
        /// Gets the input parameters for the billing calculations
        /// Each yield statement represent a seperate tests run
        /// </summary>
        /// <returns>An object array representing all 3 inputs to the test method.</returns>
        public static IEnumerable<object[]> GetBillingInputsWithNewEventsForBillOverride()
        {
            // Test billing overrides with various tiers
            yield return new object[] { BillingEventsInput, BillingSummaryInput, BillingOverrideGlobal, 0 };
            yield return new object[] { BillingEventsInput, BillingSummaryInput, BillingOverrideAccount, 0 };
            yield return new object[] { BillingEventsInput, BillingSummaryInput, BillingOverrideSKU, 0 };
            yield return new object[] { BillingEventsInput, BillingSummaryInput, BillingOverrideSubscription, 0 };

            // Test billing overrides that are shorter term
            yield return new object[] { BillingEventsInput, BillingSummaryInput, BillingOverrideGlobalSmall, 508 };
            //
        }



        /// <summary>
        /// Gets input parameters for testing billing calculations when
        /// no new events exist in the current billing timeperiod (1 hr)
        /// </summary>
        /// <returns>An object array representing all 3 inputs to the test method.</returns>
        public static IEnumerable<object[]> GetBillingInputsNoNewEvents()
        {
            // Test billing calculations with Available, Shutdown, and Deleted environments in
            // the previous billing summary. No current billing events exist. The Deleted environments
            // will not add to the new billing summary's total billable units.
            yield return new object[]
            {
                // Current billing summary is null
                null,
                BillingSummaryOutputNoCurrentEvents
            };
        }

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

        [Theory]
        [MemberData(nameof(GetBillingInputsWithNewEvents))]
        public async Task BillingSummaryIsCreatedFromEvents(
            IEnumerable<BillingEvent> inputEvents,
            BillingSummary inputSummary,
            BillingSummary expectedSummary)
        {
            foreach (var input in inputEvents)
            {
                await repository.CreateAsync(input, logger);
            }
            //TODO: We should restructure these tests so that time can be an actual measurement that we calculate.
            var shardUsageTimes = new Dictionary<string, double>();
            // Billing Service
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes);

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
            Assert.Equal(expectedEnvironmentUsageDetail.UserId, actualEnvironmentUsageDetail.UserId);

            // UserId list match
            Assert.NotNull(expectedSummary.UsageDetail.Users);
            Assert.Equal(expectedSummary.UsageDetail.Users.Count, actualSummary.UsageDetail.Users.Count);
        }

        [Theory]
        [MemberData(nameof(GetBillingInputsWithNewEvents))]
        public async Task BillingSummaryIsCreatedFromEvents_GlobalOverride(
           IEnumerable<BillingEvent> inputEvents,
           BillingSummary inputSummary,
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
           BillingSummary expectedSummary)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
        {
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
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes);

            // Compare total billable units
            // Should be overriden with 0
            Assert.Equal(0, actualSummary.Usage.First().Value, 2);
        }

        [Theory]
        [MemberData(nameof(GetBillingInputsWithNewEventsForBillOverride))]
        public async Task BillingSummaryIsCreatedFromEvents_variousOverrides(
            IEnumerable<BillingEvent> inputEvents,
            BillingSummary inputSummary,
            BillingOverride billingOverride,
            double expectedBilledTime)
        {
            foreach (var input in inputEvents)
            {
                await repository.CreateAsync(input, logger);
            }
            await overrideRepository.CreateAsync(billingOverride, logger);
            var shardUsageTimes = new Dictionary<string, double>();
            // Billing Service
            var actualSummary = await billingService.CalculateBillingUnits(testPlan, inputEvents, inputSummary, TestTimeNow, AzureLocation.WestUs2, shardUsageTimes);

            // Compare total billable units
            // Should be overriden with 0
            Assert.Equal(expectedBilledTime, actualSummary.Usage.First().Value, 2);
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
                                    UserId = testEnvironment.UserId,
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
                                    UserId = testEnvironment.UserId,
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
                                    UserId = testEnvironment.UserId,
                                    Usage = new Dictionary<string, double>
                                    {
                                        { WestUs2MeterId, 0 },
                                    },
                                }
                            }
                        },
                        Users = new Dictionary<string, UserUsageDetail>
                        {
                            {
                                testEnvironment.UserId,
                                new UserUsageDetail
                                {
                                    Usage = new Dictionary<string, double>
                                    {
                                        { WestUs2MeterId, 0 },
                                    },

                                }
                            }
                        }

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
                            UserId = testEnvironment.UserId,
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
                            UserId = testEnvironment.UserId,
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, billableShutdownHours },
                            },
                        }
                    },
                },
                    Users = new Dictionary<string, UserUsageDetail>
                {
                    {
                        testEnvironment.UserId,
                        new UserUsageDetail
                        {
                            Usage = new Dictionary<string, double>
                            {
                                { WestUs2MeterId, billableActiveHours + billableShutdownHours },
                            },

                        }
                    }
                }

                }
            };
            var shardUsageTimes = new Dictionary<string, double>();
            // BIlling Service
            var actualSummary = await billingService.CaculateBillingForEnvironmentsWithNoEvents(testPlan,
                                                                                        null,
                                                                                        latestBillingEvent,
                                                                                        TestTimeNow,
                                                                                        AzureLocation.WestUs2,
                                                                                        shardUsageTimes);

            // Compare total billable units
            Assert.Equal(expectedSummary.Usage.First().Value, actualSummary.Usage.First().Value, 2);

            // MeterId match
            Assert.Equal(expectedSummary.Usage.First().Key, actualSummary.Usage.First().Key);

            // UserId list match
            Assert.NotNull(expectedSummary.UsageDetail.Users);
            Assert.Equal(expectedSummary.UsageDetail.Users.Count, actualSummary.UsageDetail.Users.Count);

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

            // UserID should match
            Assert.Equal(expectedAvailableEnvironmentUsageDetail.UserId, actualAvailableEnvironmentUsageDetail.UserId);
            Assert.Equal(expectedShutdownEnvironment2UsageDetail.UserId, actualShutdownEnvironmentUsageDetail.UserId);

        }

        [Fact]
        public async Task GenerateBill_NoStateChange()
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
                                    UserId = testEnvironment.UserId,
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
        public async Task GenerateBill_StateChangeInMiddle()
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
        public async Task GenerateBill_StateChangeInMiddleTheDeleted()
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
        public async Task GenerateBill_DeletedBeforeTimeRange()
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

            await RunGenerateBillingSummaryTests(plan, allBillingEvents, oldBillingEvents, expectedUsage, startTime, lastSummaryEndTime, endTime);
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
            await sut.BeginAccountCalculations(plan.Plan, start, endBillingTime, logger.Object, AzureLocation.WestUs2, shardTimes);

            var resultSummary = argsInput as BillingSummary;

            Assert.NotNull(resultSummary);
            Assert.Equal(endBillingTime, resultSummary.PeriodEnd);
            Assert.Equal(lastSummaryTime, resultSummary.PeriodStart);

            Assert.Equal(1, resultSummary.Usage.Count);
            var usageTotal = resultSummary.Usage.Values.First();
            Assert.Equal(expectedUsage, usageTotal, 4);
        }


        private Mock<ISkuCatalog> GetMockSKuCatalog()
        {
            var mockStandardLinux = new Mock<ICloudEnvironmentSku>();
            mockStandardLinux.Setup(sku => sku.ComputeVsoUnitsPerHour).Returns(standardLinuxComputeUnitPerHr);
            mockStandardLinux.Setup(sku => sku.StorageVsoUnitsPerHour).Returns(standardLinuxStorageUnitPerHr);

            var mockPremiumLinux = new Mock<ICloudEnvironmentSku>();
            mockPremiumLinux.Setup(sku => sku.ComputeVsoUnitsPerHour).Returns(premiumLinuxComputeUnitPerHr);
            mockPremiumLinux.Setup(sku => sku.StorageVsoUnitsPerHour).Returns(premiumLinuxStorageUnitPerHr);

            var skus = new Dictionary<string, ICloudEnvironmentSku>
            {
                [standardLinuxSkuName] = mockStandardLinux.Object,
                [premiumLinuxSkuName] = mockPremiumLinux.Object,
            };
            var mockSkuCatelog = new Mock<ISkuCatalog>();
            mockSkuCatelog.Setup(cat => cat.CloudEnvironmentSkus).Returns(skus);
            return mockSkuCatelog;
        }
    }
}
