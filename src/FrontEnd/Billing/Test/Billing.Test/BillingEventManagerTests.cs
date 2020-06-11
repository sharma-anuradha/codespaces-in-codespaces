using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Moq;
using Xunit;
using UsageDictionary = System.Collections.Generic.Dictionary<string, double>;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Test
{
    public class BillingEventManagerTests : BaseBillingTests
    {
        public BillingEventManagerTests()
        {
            // Uncomment to use the local CosmosDB emulator instead of in-memory mock.
            ////this.repository = ConfigureEmulator();
        }

        private IBillingEventRepository ConfigureEmulator()
        {
            var healthProvider = new HealthProvider();

            const string emulatorUrl = "https://localhost:8081";
            const string emulatorAuthKey =
                "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            var clientOptions = new DocumentDbClientOptions
            {
                HostUrl = emulatorUrl,
                AuthKey = emulatorAuthKey,
                DatabaseId = nameof(BillingEventManagerTests),
            };
            var clientProvider = new DocumentDbClientProvider(
                Options.Create(clientOptions),
                healthProvider,
                loggerFactory,
                defaultLogValues: null);

            var collectionOptions = new DocumentDbCollectionOptions();
            BillingEventRepository.ConfigureOptions(collectionOptions);
            var optionsMonitor = new Mock<IOptionsMonitor<DocumentDbCollectionOptions>>();
            optionsMonitor.Setup(o => o.Get(It.IsAny<string>())).Returns(collectionOptions);
            return new BillingEventRepository(
                optionsMonitor.Object,
                clientProvider,
                healthProvider,
                loggerFactory,
                defaultLogValues: null);
        }

        [Fact]
        public async Task CreateEvent()
        {
            var stateChange = new BillingStateChange
            {
                OldValue = "one",
                NewValue = "two",
            };
            var bev = await manager.CreateEventAsync(
                testPlan,
                testEnvironment,
                BillingEventTypes.EnvironmentStateChange,
                stateChange,
                logger);
            Assert.NotNull(bev);
            Assert.NotNull(bev.Id);
            Assert.Equal(DateTimeKind.Utc, bev.Time.Kind);
        }

        private async Task GetEvents(string[] eventTypes)
        {
            var startTime = DateTime.UtcNow;

            const int count = 5;
            for (var i = 1; i <= count; i++)
            {
                var stateChange = new BillingStateChange
                {
                    OldValue = (i - 1).ToString(),
                    NewValue = i.ToString(),
                };
                await manager.CreateEventAsync(
                    testPlan,
                    testEnvironment,
                    BillingEventTypes.EnvironmentStateChange,
                    stateChange,
                    logger);

                // Ensure the event timestamps are distinguishable.
                await Task.Delay(1);
            }

            var allEvents = (await manager.GetPlanEventsAsync(
                testPlan,
                startTime,
                DateTime.UtcNow,
                eventTypes,
                logger)).ToList();

            Assert.Equal(count, allEvents.Count);

            // Validate that the returned events are in order in terms of time and values.
            allEvents.Aggregate<BillingEvent>((previous, current) =>
            {
                Assert.True(previous.Time < current.Time);

                var previousChange = (BillingStateChange)previous.Args;
                var currentChange = (BillingStateChange)current.Args;
                Assert.True(int.Parse(previousChange.NewValue) < int.Parse(currentChange.NewValue));

                return current;
            });
        }

        [Fact]
        public Task GetAllEvents() => GetEvents(null);

        [Fact]
        public Task GetEventsOfOneType() => GetEvents(new[] { BillingEventTypes.EnvironmentStateChange });

        [Fact]
        public Task GetEventsOfMultipleTypes() =>
            GetEvents(new[] { BillingEventTypes.EnvironmentStateChange, BillingEventTypes.AccountPlanChange });

        [Fact]
        public void SerializeAndDeserializeBillingStateChangeEvent()
        {
            var billingEvent = new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Time = DateTime.UtcNow,
                Plan = testPlan,
                Environment = testEnvironment,
                Type = BillingEventTypes.EnvironmentStateChange,
                Args = new BillingStateChange
                {
                    OldValue = "1",
                    NewValue = "2",
                },
            };

            string billingEventJson;
            using (var stringWriter = new StringWriter())
            {
                serializer.Serialize(stringWriter, billingEvent);
                billingEventJson = stringWriter.ToString();
            }

            BillingEvent billingEvent2;
            using (var stringReader = new StringReader(billingEventJson))
            {
                billingEvent2 = (BillingEvent)serializer.Deserialize(stringReader, typeof(BillingEvent));
            }

            Assert.NotNull(billingEvent2);
            Assert.Equal(billingEvent.Type, billingEvent2.Type);
            Assert.IsType<BillingStateChange>(billingEvent2.Args);
            Assert.Equal("1", ((BillingStateChange)billingEvent2.Args).OldValue);
            Assert.Equal("2", ((BillingStateChange)billingEvent2.Args).NewValue);
        }

        [Fact]
        public void SerializeAndDeserializeBillingSummaryEvent()
        {
            var now = DateTime.UtcNow;
            var meter = Guid.NewGuid().ToString();

            var usageDetail = new UsageDetail
            {
                Environments = new Dictionary<string, EnvironmentUsageDetail>
                {
                    ["1"] = new EnvironmentUsageDetail
                    {
                        Name = "one",
                        EndState = "Running",
                        Usage = new UsageDictionary { [meter] = 1.1 },
                        Sku = testEnvironment.Sku
                    },
                    ["2"] = new EnvironmentUsageDetail
                    {
                        Name = "two",
                        EndState = "Suspended",
                        Usage = new UsageDictionary { [meter] = 2.2 },
                        Sku = testEnvironment.Sku
                    },
                },
            };

            var billingSummary = new BillingSummary
            {
                PeriodStart = now.AddMinutes(-1),
                PeriodEnd = now,
                Plan = "test",
                SubscriptionState = SubscriptionStates.Registered,
                Usage = new UsageDictionary { [meter] = 3.3 },
                UsageDetail = usageDetail,
                SubmissionState = BillingSubmissionState.None,
            };

            var billingEvent = new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Time = DateTime.UtcNow,
                Plan = testPlan,
                Environment = testEnvironment,
                Type = BillingEventTypes.BillingSummary,
                Args = billingSummary,
            };

            string billingEventJson;
            using (var stringWriter = new StringWriter())
            {
                serializer.Serialize(stringWriter, billingEvent);
                billingEventJson = stringWriter.ToString();
            }

            BillingEvent billingEvent2;
            using (var stringReader = new StringReader(billingEventJson))
            {
                billingEvent2 = (BillingEvent)serializer.Deserialize(stringReader, typeof(BillingEvent));
            }

            Assert.NotNull(billingEvent2);
            Assert.Equal(billingEvent.Type, billingEvent2.Type);
            Assert.IsType<BillingSummary>(billingEvent2.Args);

            var billingSummary2 = (BillingSummary)billingEvent2.Args;
            Assert.Equal(billingSummary.PeriodStart, billingSummary2.PeriodStart);
            Assert.Equal(billingSummary.PeriodEnd, billingSummary2.PeriodEnd);
            Assert.Equal(billingSummary.Plan, billingSummary2.Plan);
            Assert.Equal(billingSummary.SubscriptionState, billingSummary2.SubscriptionState);

            Assert.NotNull(billingSummary2.Usage);
            Assert.Equal(billingSummary.Usage.Count, billingSummary2.Usage.Count);

            var usageDetail2 = billingSummary2.UsageDetail;
            Assert.NotNull(usageDetail2);
            Assert.NotNull(usageDetail2.Environments);
            Assert.Equal(usageDetail.Environments.Count, usageDetail2.Environments.Count);
        }

        [Fact]
        public async Task GetPlanEvents()
        {
            var startTime = await CreateMockBillingData(includeSummaries: true);

            var accountEvents = (await manager.GetPlanEventsAsync(
                testPlan, startTime, DateTime.UtcNow, eventTypes: null, logger)).ToList();

            Assert.Equal(3, accountEvents.Count);
            Assert.True(accountEvents.All(bev => bev.Plan == testPlan));

            var accountEvents2 = (await manager.GetPlanEventsAsync(
                testPlan2, startTime, DateTime.UtcNow, eventTypes: null, logger)).ToList();

            Assert.Equal(5, accountEvents2.Count);
            Assert.True(accountEvents2.All(bev => bev.Plan == testPlan2));
        }

        private async Task<DateTime> CreateMockBillingData(bool includeSummaries)
        {
            await manager.CreateEventAsync(
                testPlan3,
                testEnvironment,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "zero", NewValue = "one" },
                logger);

            await manager.CreateEventAsync(
                testPlan,
                testEnvironment,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "zero", NewValue = "one" },
                logger);

            await Task.Delay(1);
            var startTime = DateTime.UtcNow;
            
            await manager.CreateEventAsync(
                testPlan,
                testEnvironment,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "one", NewValue = "two" },
                logger);
            await manager.CreateEventAsync(
                testPlan,
                testEnvironment2,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "two", NewValue = "three" },
                logger);

            await manager.CreateEventAsync(
                testPlan2,
                testEnvironment,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "one", NewValue = "two" },
                logger);
            await manager.CreateEventAsync(
                testPlan2,
                testEnvironment2,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "two", NewValue = "three" },
                logger);

            if (includeSummaries)
            {
                await Task.Delay(1);
                var summaryTime = DateTime.UtcNow;

                await manager.CreateEventAsync(
                    testPlan,
                    null,
                    BillingEventTypes.BillingSummary,
                    new BillingSummary
                    {
                        PeriodStart = startTime,
                        PeriodEnd = summaryTime,
                        Plan = string.Empty,
                        SubscriptionState = string.Empty,
                        Usage = new UsageDictionary(),
                        UsageDetail = new UsageDetail
                        {
                            Environments = new Dictionary<string, EnvironmentUsageDetail>
                            {
                                [testEnvironment.Id] = new EnvironmentUsageDetail
                                {
                                    Name = testEnvironment.Name,
                                    Usage = new UsageDictionary(),
                                    EndState = "two",
                                    Sku = testEnvironment.Sku
                                },
                                [testEnvironment2.Id] = new EnvironmentUsageDetail
                                {
                                    Name = testEnvironment2.Name,
                                    Usage = new UsageDictionary(),
                                    EndState = "three",
                                    Sku = testEnvironment.Sku
                                },
                            },
                        },
                        SubmissionState = BillingSubmissionState.None,
                    },
                    logger); ;
                await manager.CreateEventAsync(
                    testPlan2,
                    null,
                    BillingEventTypes.BillingSummary,
                    new BillingSummary
                    {
                        PeriodStart = startTime,
                        PeriodEnd = summaryTime,
                        Plan = string.Empty,
                        SubscriptionState = string.Empty,
                        Usage = new UsageDictionary(),
                        UsageDetail = new UsageDetail
                        {
                            Environments = new Dictionary<string, EnvironmentUsageDetail>
                            {
                                [testEnvironment.Id] = new EnvironmentUsageDetail
                                {
                                    Name = testEnvironment.Name,
                                    Usage = new UsageDictionary(),
                                    EndState = "two",
                                    Sku = testEnvironment.Sku
                                },
                                [testEnvironment2.Id] = new EnvironmentUsageDetail
                                {
                                    Name = testEnvironment2.Name,
                                    Usage = new UsageDictionary(),
                                    EndState = "three",
                                    Sku = testEnvironment.Sku
                                },
                            }
                        },
                        SubmissionState = BillingSubmissionState.None,
                    },
                    logger);
            }

            await Task.Delay(1);

            await manager.CreateEventAsync(
                testPlan2,
                testEnvironment2,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "two", NewValue = "four" },
                logger);
            await manager.CreateEventAsync(
                testPlan2,
                testEnvironment2,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "four", NewValue = "five" },
                logger);

            await Task.Delay(1);

            return startTime;
        }
    }
}
