using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tests
{
    public class BillingEventManagerTests
    {
        private static readonly string subscription = Guid.NewGuid().ToString();
        private static readonly VsoAccountInfo testAccount = new VsoAccountInfo
        {
            Subscription = subscription,
            ResourceGroup = "testRG",
            Name = "testAccount",
            Location = AzureLocation.WestUs2,
        };
        private static readonly VsoAccountInfo testAccount2 = new VsoAccountInfo
        {
            Subscription = subscription,
            ResourceGroup = "testRG",
            Name = "testAccount2",
            Location = AzureLocation.WestUs2,
        };
        private static readonly VsoAccountInfo testAccount3 = new VsoAccountInfo
        {
            Subscription = subscription,
            ResourceGroup = "testRG",
            Name = "testAccount3",
            Location = AzureLocation.WestUs2,
        };
        private static readonly EnvironmentBillingInfo testEnvironment = new EnvironmentBillingInfo
        {
            Id = Guid.NewGuid().ToString(),
            Name = "testEnvironment",
            UserId = Guid.NewGuid().ToString(),
            Sku = new Sku { Name = "testSku", Tier = "test" },
        };
        private static readonly EnvironmentBillingInfo testEnvironment2 = new EnvironmentBillingInfo
        {
            Id = Guid.NewGuid().ToString(),
            Name = "testEnvironment2",
            UserId = testEnvironment.UserId,
            Sku = new Sku { Name = "testSku", Tier = "test" },
        };

        private readonly IBillingEventRepository repository;
        private readonly BillingEventManager manager;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly IDiagnosticsLogger logger;
        private readonly JsonSerializer serializer;

        public BillingEventManagerTests()
        {
            this.loggerFactory = new DefaultLoggerFactory();
            this.logger = loggerFactory.New();

            this.repository = new MockBillingEventRepository();

            // Uncomment to use the local CosmosDB emulator instead of in-memory mock.
            ////this.repository = ConfigureEmulator();

            this.manager = new BillingEventManager(this.repository);
            this.serializer = JsonSerializer.CreateDefault();
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
            return new BillingEventRepository(
                Options.Create(collectionOptions),
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
            var bev = await this.manager.CreateEventAsync(
                testAccount,
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
            for (int i = 1; i <= count; i++)
            {
                var stateChange = new BillingStateChange
                {
                    OldValue = (i - 1).ToString(),
                    NewValue = i.ToString(),
                };
                await this.manager.CreateEventAsync(
                    testAccount,
                    testEnvironment,
                    BillingEventTypes.EnvironmentStateChange,
                    stateChange,
                    logger);

                // Ensure the event timestamps are distinguishable.
                await Task.Delay(1);
            }

            var allEvents = (await this.manager.GetAccountEventsAsync(
                testAccount,
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
                Account = testAccount,
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
                        Usage = new Dictionary<string, double> { [meter] = 1.1 },
                    },
                    ["2"] = new EnvironmentUsageDetail
                    {
                        Name = "two",
                        EndState = "Suspended",
                        Usage = new Dictionary<string, double> { [meter] = 2.2 },
                    },
                },
                Users = new Dictionary<string, UserUsageDetail>
                {
                    ["test"] = new UserUsageDetail
                    {
                        Usage = new Dictionary<string, double> { [meter] = 3.3 },
                    },
                },
            };

            var billingSummary = new BillingSummary
            {
                PeriodStart = now.AddMinutes(-1),
                PeriodEnd = now,
                Plan = "test",
                SubscriptionState = SubscriptionStates.Registered,
                Usage = new Dictionary<string, double> { [meter] = 3.3 },
                UsageDetail = usageDetail,
                Emitted = false,
            };

            var billingEvent = new BillingEvent
            {
                Id = Guid.NewGuid().ToString(),
                Time = DateTime.UtcNow,
                Account = testAccount,
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
            Assert.NotNull(usageDetail2.Users);
            Assert.Equal(usageDetail.Users.Count, usageDetail2.Users.Count);
        }

        [Fact]
        public async Task GetAccounts()
        {
            var startTime = await CreateMockBillingData(includeSummaries: true);

            var accounts = (await this.manager.GetAccountsAsync(startTime, DateTime.UtcNow, logger)).ToList();

            Assert.Equal(2, accounts.Count);
            Assert.Contains(testAccount, accounts);
            Assert.Contains(testAccount2, accounts);
        }

        [Fact]
        public async Task GetAccountEvents()
        {
            var startTime = await CreateMockBillingData(includeSummaries: true);

            var accountEvents = (await this.manager.GetAccountEventsAsync(
                testAccount, startTime, DateTime.UtcNow, eventTypes: null, logger)).ToList();

            Assert.Equal(3, accountEvents.Count);
            Assert.True(accountEvents.All(bev => bev.Account == testAccount));

            var accountEvents2 = (await this.manager.GetAccountEventsAsync(
                testAccount2, startTime, DateTime.UtcNow, eventTypes: null, logger)).ToList();

            Assert.Equal(5, accountEvents2.Count);
            Assert.True(accountEvents2.All(bev => bev.Account == testAccount2));
        }

        private async Task<DateTime> CreateMockBillingData(bool includeSummaries)
        {
            await this.manager.CreateEventAsync(
                testAccount3,
                testEnvironment,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "zero", NewValue = "one" },
                logger);

            await this.manager.CreateEventAsync(
                testAccount,
                testEnvironment,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "zero", NewValue = "one" },
                logger);

            await Task.Delay(1);
            DateTime startTime = DateTime.UtcNow;
            
            await this.manager.CreateEventAsync(
                testAccount,
                testEnvironment,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "one", NewValue = "two" },
                logger);
            await this.manager.CreateEventAsync(
                testAccount,
                testEnvironment2,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "two", NewValue = "three" },
                logger);

            await this.manager.CreateEventAsync(
                testAccount2,
                testEnvironment,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "one", NewValue = "two" },
                logger);
            await this.manager.CreateEventAsync(
                testAccount2,
                testEnvironment2,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "two", NewValue = "three" },
                logger);

            if (includeSummaries)
            {
                await Task.Delay(1);
                var summaryTime = DateTime.UtcNow;

                await this.manager.CreateEventAsync(
                    testAccount,
                    null,
                    BillingEventTypes.BillingSummary,
                    new BillingSummary
                    {
                        PeriodStart = startTime,
                        PeriodEnd = summaryTime,
                        Plan = string.Empty,
                        SubscriptionState = string.Empty,
                        Usage = new Dictionary<string, double>(),
                        UsageDetail = new UsageDetail
                        {
                            Environments = new Dictionary<string, EnvironmentUsageDetail>
                            {
                                [testEnvironment.Id] = new EnvironmentUsageDetail
                                {
                                    Name = testEnvironment.Name,
                                    Usage = new Dictionary<string, double>(),
                                    EndState = "two",
                                },
                                [testEnvironment2.Id] = new EnvironmentUsageDetail
                                {
                                    Name = testEnvironment2.Name,
                                    Usage = new Dictionary<string, double>(),
                                    EndState = "three",
                                },
                            },
                            Users = new Dictionary<string, UserUsageDetail>
                            {
                                [testEnvironment.UserId] = new UserUsageDetail
                                {
                                    Usage = new Dictionary<string, double>(),
                                }
                            },
                        },
                        Emitted = false,
                    },
                    logger); ;
                await this.manager.CreateEventAsync(
                    testAccount2,
                    null,
                    BillingEventTypes.BillingSummary,
                    new BillingSummary
                    {
                        PeriodStart = startTime,
                        PeriodEnd = summaryTime,
                        Plan = string.Empty,
                        SubscriptionState = string.Empty,
                        Usage = new Dictionary<string, double>(),
                        UsageDetail = new UsageDetail
                        {
                            Environments = new Dictionary<string, EnvironmentUsageDetail>
                            {
                                [testEnvironment.Id] = new EnvironmentUsageDetail
                                {
                                    Name = testEnvironment.Name,
                                    Usage = new Dictionary<string, double>(),
                                    EndState = "two",
                                },
                                [testEnvironment2.Id] = new EnvironmentUsageDetail
                                {
                                    Name = testEnvironment2.Name,
                                    Usage = new Dictionary<string, double>(),
                                    EndState = "three",
                                },
                            },
                            Users = new Dictionary<string, UserUsageDetail>
                            {
                                [testEnvironment.UserId] = new UserUsageDetail
                                {
                                    Usage = new Dictionary<string, double>(),
                                }
                            },
                        },
                        Emitted = false,
                    },
                    logger);
            }

            await Task.Delay(1);

            await this.manager.CreateEventAsync(
                testAccount2,
                testEnvironment2,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "two", NewValue = "four" },
                logger);
            await this.manager.CreateEventAsync(
                testAccount2,
                testEnvironment2,
                BillingEventTypes.EnvironmentStateChange,
                new BillingStateChange { OldValue = "four", NewValue = "five" },
                logger);

            await Task.Delay(1);

            return startTime;
        }
    }
}
