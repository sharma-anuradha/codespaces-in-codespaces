using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Test
{
    public class BillingServiceTests : BaseBillingTests
    {
        private static readonly DateTime TestTimeNow = DateTime.UtcNow;
        private readonly BillingService billingService;
        private readonly decimal smallLinuxComputeUnitPerHr = 125;
        private readonly decimal smallLinuxStorageUnitPerHr = 2;
        private static readonly string WestUs2MeterId = "5f3afa79-01ad-4d7e-b691-73feca4ea350";
        
        // 5 hrs Available => 127 * 5 = 635
        private static readonly double BillableUnits = 635;
        // 3 hrs Available + 3hrs Shutdown => 127units * 3hrs + 2units * 3 hrs = 387
        private static readonly double BillableUnitsWithShutdown = 387;
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
                Account = testAccount,
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
                Account = testAccount,
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
                Account = testAccount,
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
                Account = testAccount,
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
                Account = testAccount,
                Args = new BillingStateChange
                {
                    OldValue = nameof(CloudEnvironmentState.Shutdown),
                    NewValue = nameof(CloudEnvironmentState.Deleted),
                },
                Environment = testEnvironment,
                Time = DateTime.UtcNow,
                Type = BillingEventTypes.EnvironmentStateChange

            },
        };
        public static readonly IEnumerable<BillingEvent> BillingEventsWithMultiEnvironmentsInput = new List<BillingEvent>
        {
            new BillingEvent
            {
                Account = testAccount,
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
                Account = testAccount,
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
                Account = testAccount,
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
                Account = testAccount,
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
                            Sku = new Sku { Name = smallLinuxSKuName },
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
        public static readonly BillingSummary BillingSummaryInputNoCurrentEvents = new BillingSummary
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
                            Sku = new Sku { Name = smallLinuxSKuName },
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
                            Sku = new Sku { Name = smallLinuxSKuName },
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
                            Sku = new Sku { Name = smallLinuxSKuName },
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
        public static readonly BillingSummary BillingSummaryOutputNoCurrentEvents = new BillingSummary
        {
            SubmissionState = BillingSubmissionState.None,
            Usage = new Dictionary<string, double>
            {
                { WestUs2MeterId, BIllableUnitsNoNewEvents },
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

            // Test billing calculations with Available and Shutdown events on 2 environments.
            yield return new object[] { BillingEventsWithMultiEnvironmentsInput, BillingSummaryInput, BillingSummaryMultiOutput };

            // Test billing calculations with Available and Deleted events and No previous billing summary.
            BillingSummaryInput.UsageDetail = null;
            yield return new object[] { BillingEventsInput, BillingSummaryInput, BillingSummaryOutput };

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
                new BillingEvent
                {
                    Args = BillingSummaryInputNoCurrentEvents,
                    // Last billing summary was written 5 hrs ago.
                    Time = TestTimeNow.Subtract(TimeSpan.FromHours(5)),
                },
                BillingSummaryOutputNoCurrentEvents
            };
        }

        public BillingServiceTests()
        {
            var mockSku = new Mock<ICloudEnvironmentSku>();
            mockSku.Setup(sku => sku.ComputeVsoUnitsPerHour).Returns(smallLinuxComputeUnitPerHr);
            mockSku.Setup(sku => sku.StorageVsoUnitsPerHour).Returns(smallLinuxStorageUnitPerHr);
            var skus = new Dictionary<string, ICloudEnvironmentSku>
            {
                [smallLinuxSKuName] = mockSku.Object
            };
            var mockSkuCatelog = new Mock<ISkuCatalog>();
            mockSkuCatelog.Setup(cat => cat.CloudEnvironmentSkus).Returns(skus);
            billingService = new BillingService(manager, 
                                            new Mock<IControlPlaneInfo>().Object, 
                                            mockSkuCatelog.Object, 
                                            logger, 
                                            new Mock<IClaimedDistributedLease>().Object);
        }

        [Theory]
        [MemberData(nameof(GetBillingInputsWithNewEvents))]
        public void BillingSummaryIsCreatedFromEvents(
            IEnumerable<BillingEvent> inputEvents, 
            BillingSummary inputSummary, 
            BillingSummary expectedSummary)
        {
            // Billing Service
            var actualSummary = billingService.CalculateBillingUnits(testAccount, inputEvents, inputSummary, TestTimeNow);

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
        [MemberData(nameof(GetBillingInputsNoNewEvents))]
        public void BillingSummaryIsCreatedNoNewEvents(
            BillingSummary currentSummary,
            BillingEvent latestBillingEvent,
            BillingSummary expectedSummary)
        {
            // BIlling Service
            var actualSummary = billingService.CaculateBillingForEnvironmentsWithNoEvents(testAccount, 
                                                                                        currentSummary, 
                                                                                        latestBillingEvent, 
                                                                                        TestTimeNow);

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
    }
}
