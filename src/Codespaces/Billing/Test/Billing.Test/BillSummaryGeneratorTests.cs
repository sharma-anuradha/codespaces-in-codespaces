using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Test
{
    public class BillSummaryGeneratorTests
    {
        private readonly decimal standardLinuxComputeUnitPerHr = 125;
        private readonly decimal premiumLinuxComputeUnitPerHr = 242;
        private readonly decimal standardLinuxStorageUnitPerHr = 2;
        private readonly decimal premiumLinuxStorageUnitPerHr = 3;
        private readonly int standadLinuxStorageSizeInGB = 32;
        private readonly int premiumLinuxStorageSizeInGB = 64;
        public static readonly string standardLinuxSkuName = "standardLinuxSku";
        public static readonly string premiumLinuxSkuName = "premiumLinuxSku";
        private static readonly IDiagnosticsLogger Logger = new DefaultLoggerFactory().New();

        private Mock<BillingSettings> BillingSettings { get; }

        private Mock<IBillSummaryManager> BillSummaryManager { get; }

        private Mock<IEnvironmentStateChangeManager> EnvironmentStateChangeManager { get; }

        private Mock<IBillingMeterService> BillingMeterService { get; }

        private Mock<IBillingSubmissionCloudStorageFactory> BillingStorageFactory { get; }

        private Mock<IPartnerCloudStorageFactory> PartnerCloudStorageFactory { get; }

        private BillSummaryGenerator BillSummaryGenerator { get; }

        public BillSummaryGeneratorTests()
        {
            BillSummaryManager = new Mock<IBillSummaryManager>();
            EnvironmentStateChangeManager = new Mock<IEnvironmentStateChangeManager>();
            BillingMeterService = new Mock<IBillingMeterService>();
            BillingStorageFactory = new Mock<IBillingSubmissionCloudStorageFactory>();
            PartnerCloudStorageFactory = new Mock<IPartnerCloudStorageFactory>();
            BillingSettings = new Mock<BillingSettings>();
            
            var skuCatalog = GetMockSKuCatalog();
            BillSummaryGenerator = new BillSummaryGenerator(BillingSettings.Object, BillSummaryManager.Object, EnvironmentStateChangeManager.Object, skuCatalog.Object, BillingMeterService.Object, BillingStorageFactory.Object, PartnerCloudStorageFactory.Object);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_GenerateForNoEventsOrEnvironments()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)null));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up an empty environment table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(Enumerable.Empty<EnvironmentStateChange>()));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(new Dictionary<string, double>());
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour.AddHours(1).AddDays(-2), returnedBill.PeriodStart);
            Assert.Equal(0, returnedBill.UsageDetail.Count);
            Assert.Equal(0, returnedBill.Usage.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithNoEnvironments()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>(),
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up an empty environment table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(Enumerable.Empty<EnvironmentStateChange>()));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(new Dictionary<string, double>());
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(0, returnedBill.UsageDetail.Count);
            Assert.Equal(0, returnedBill.Usage.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillButAskedToGenerateSameOneAgain_Error()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            var alreadyGeneratedBill = new BillSummary()
            {
                PeriodStart = topOfCurrHour,
                PeriodEnd = topOfCurrHour.AddHours(1),
                UsageDetail = new List<EnvironmentUsage>(),
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)alreadyGeneratedBill));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up an empty environment table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(Enumerable.Empty<EnvironmentStateChange>()));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(new Dictionary<string, double>());
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify that no bill was generated
            Assert.Null(returnedBill);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillButAskedToGenerateFarInPast_Error()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(-24),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>(),
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up an empty environment table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(Enumerable.Empty<EnvironmentStateChange>()));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(new Dictionary<string, double>());
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify that no bill was generated
            Assert.Null(returnedBill);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithOneActiveEnvironment()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up an empty environment table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(Enumerable.Empty<EnvironmentStateChange>()));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithOneSuspendedEnvironment()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Shutdown",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up an empty environment table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(Enumerable.Empty<EnvironmentStateChange>()));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(0, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithOneArchivedEnvironment()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Archived",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up an empty environment table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(Enumerable.Empty<EnvironmentStateChange>()));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(0, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithOneDeletedEnvironment()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Deleted",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up an empty environment table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(Enumerable.Empty<EnvironmentStateChange>()));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(0, returnedBill.Usage.Count);
            Assert.Equal(0, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_NoPreviousBillWithOneActiveEnvironmentCreated()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            var newEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var newEnvironmentState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = newEnvironment,
                OldValue = null,
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)null));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { newEnvironmentState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour.AddHours(1).AddDays(-2), returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_NoPreviousBillWithOneSuspendedEnvironmentCreated()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            var newEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var newEnvironmentState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = newEnvironment,
                OldValue = null,
                NewValue = "Shutdown"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)null));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { newEnvironmentState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour.AddHours(1).AddDays(-2), returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
            Assert.Equal(0, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeToAvailable()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Shutdown",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var newEnvironmentState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myEnvironment,
                OldValue = "Shutdown",
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { newEnvironmentState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeToShutdown()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var newEnvironmentState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { newEnvironmentState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeSuspendedToArchived()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Shutdown",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var newEnvironmentState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myEnvironment,
                OldValue = "Shutdown",
                NewValue = "Archived"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { newEnvironmentState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(0, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeArchivedToAvailable()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Archived",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var newEnvironmentState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myEnvironment,
                OldValue = "Archived",
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { newEnvironmentState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeAvailableToDeleted()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var newEnvironmentState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myEnvironment,
                OldValue = "Available",
                NewValue = "Deleted"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { newEnvironmentState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeSuspendedToDeleted()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Shutdown",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var newEnvironmentState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myEnvironment,
                OldValue = "Shutdown",
                NewValue = "Deleted"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { newEnvironmentState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(0, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeAvailableToAvailable()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var newEnvironmentState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myEnvironment,
                OldValue = "Available",
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { newEnvironmentState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeSuspendedToSuspended()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Shutdown",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var newEnvironmentState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myEnvironment,
                OldValue = "Shutdown",
                NewValue = "Shutdown"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { newEnvironmentState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(0, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeArchivedToArchived()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Archived",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var newEnvironmentState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myEnvironment,
                OldValue = "Archived",
                NewValue = "Archived"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { newEnvironmentState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(0, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeAvailableToShutdownToAvailable()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var stateChangeAvailableToShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.25),
                Environment = myEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };

            var stateChangeShutdownToAvailable = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.75),
                Environment = myEnvironment,
                OldValue = "Shutdown",
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { stateChangeAvailableToShutdown, stateChangeShutdownToAvailable })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_NoPreviousBillWithStateChangeToAvailableToDeleted()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            var newEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var stateChangeToAvailable = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = newEnvironment,
                OldValue = null,
                NewValue = "Available"
            };

            var stateChangeAvailableToDeleted = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.75),
                Environment = newEnvironment,
                OldValue = "Available",
                NewValue = "Deleted"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)null));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { stateChangeToAvailable, stateChangeAvailableToDeleted })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour.AddHours(1).AddDays(-2), returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(900d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(900d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_NoPreviousBillWithStateChangeToAvailableToSuspended()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            var newEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var stateChangeToAvailable = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = newEnvironment,
                OldValue = null,
                NewValue = "Available"
            };

            var stateChangeAvailableToShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.75),
                Environment = newEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };


            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)null));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { stateChangeToAvailable, stateChangeAvailableToShutdown })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour.AddHours(1).AddDays(-2), returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(900d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_NoPreviousBillWithStateChangeToAvailableToSuspendedToDeleted()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            var newEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var stateChangeToAvailable = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.25),
                Environment = newEnvironment,
                OldValue = null,
                NewValue = "Available"
            };

            var stateChangeAvailableToShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = newEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };

            var stateChangeShutdownToDeleted = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.75),
                Environment = newEnvironment,
                OldValue = "Shutdown",
                NewValue = "Deleted"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)null));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { stateChangeToAvailable, stateChangeAvailableToShutdown, stateChangeShutdownToDeleted })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour.AddHours(1).AddDays(-2), returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(900d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_NoPreviousBillWithStateChangeToSuspendedToDeleted()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            var newEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var stateChangeToShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.25),
                Environment = newEnvironment,
                OldValue = null,
                NewValue = "Shutdown"
            };

            var stateChangeShutdownToDeleted = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.75),
                Environment = newEnvironment,
                OldValue = "Shutdown",
                NewValue = "Deleted"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)null));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { stateChangeToShutdown, stateChangeShutdownToDeleted })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour.AddHours(1).AddDays(-2), returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
            Assert.Equal(0, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeToAvailableOnHour()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Shutdown",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var newEnvironmentState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour,
                Environment = myEnvironment,
                OldValue = "Shutdown",
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { newEnvironmentState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeToShutdownOnHour()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var newEnvironmentState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour,
                Environment = myEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { newEnvironmentState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(0, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeToAvailableOnEndHour()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Shutdown",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var newEnvironmentState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(1),
                Environment = myEnvironment,
                OldValue = "Shutdown",
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { newEnvironmentState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(0, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeToShutdownOnEndHour()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var newEnvironmentState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(1),
                Environment = myEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { newEnvironmentState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeActiveOneSecondBetweenShutdown()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Shutdown",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var stateChangeShutdownToAvailable = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myEnvironment,
                OldValue = "Shutdown",
                NewValue = "Available"
            };

            var stateChangeAvailableToShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5).AddSeconds(1),
                Environment = myEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { stateChangeShutdownToAvailable, stateChangeAvailableToShutdown })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_NoPreviousBillWithStateChangeActiveOneSecondBeforeDeleted()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            var newEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var stateChangeToAvailable = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = newEnvironment,
                OldValue = null,
                NewValue = "Available"
            };

            var stateChangeAvailableToDeleted = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5).AddSeconds(1),
                Environment = newEnvironment,
                OldValue = "Available",
                NewValue = "Deleted"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)null));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { stateChangeToAvailable, stateChangeAvailableToDeleted })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour.AddHours(1).AddDays(-2), returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentStateChangeShutdownOneSecond()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var stateChangeAvailableToShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };

            var stateChangeShutdownToAvailable = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5).AddSeconds(1),
                Environment = myEnvironment,
                OldValue = "Shutdown",
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { stateChangeAvailableToShutdown, stateChangeShutdownToAvailable })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(3599d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentSkuChangeStandardToPremium()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var myPremiumEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = premiumLinuxSkuName }
            };

            var stateChangeAvailableToShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };

            var stateChangeShutdownToAvailableNewSku = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5).AddMinutes(3),
                Environment = myPremiumEnvironment,
                OldValue = "Shutdown",
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { stateChangeAvailableToShutdown, stateChangeShutdownToAvailableNewSku })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(2, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(standardLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Sku);
            Assert.Equal(1620d, returnedBill.UsageDetail[0].ResourceUsage.Compute[1].Usage);
            Assert.Equal(premiumLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Compute[1].Sku);
            Assert.Equal(2, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1980d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(standardLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Sku);
            Assert.Equal(1620d, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Usage);
            Assert.Equal(premiumLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Sku);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentSkuChangePremiumToStandard()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = premiumLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var myPremiumEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = premiumLinuxSkuName }
            };

            var stateChangeAvailableToShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myPremiumEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };

            var stateChangeShutdownToAvailableNewSku = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5).AddMinutes(3),
                Environment = myEnvironment,
                OldValue = "Shutdown",
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { stateChangeAvailableToShutdown, stateChangeShutdownToAvailableNewSku })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(2, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(premiumLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Sku);
            Assert.Equal(1620d, returnedBill.UsageDetail[0].ResourceUsage.Compute[1].Usage);
            Assert.Equal(standardLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Compute[1].Sku);
            Assert.Equal(2, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1980d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(premiumLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Sku);
            Assert.Equal(1620d, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Usage);
            Assert.Equal(standardLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Sku);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillShutdownWithEnvironmentSkuChangeStandardToPremium()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Shutdown",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myPremiumEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = premiumLinuxSkuName }
            };

            var changeToNewSku = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myPremiumEnvironment,
                OldValue = "Shutdown",
                NewValue = "Shutdown"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { changeToNewSku })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);

            Assert.Equal(2, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(standardLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Sku);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Usage);
            Assert.Equal(premiumLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Sku);
            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentSkuChangeStandardToPremiumToStandardWhileShutdown()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var myPremiumEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = premiumLinuxSkuName }
            };

            var stateChangeAvailableToShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };

            var skuChangeShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5).AddMinutes(3),
                Environment = myPremiumEnvironment,
                OldValue = "Shutdown",
                NewValue = "Shutdown"
            };

            var stateChangeShutdownToAvailable = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5).AddMinutes(6),
                Environment = myEnvironment,
                OldValue = "Shutdown",
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { stateChangeAvailableToShutdown, skuChangeShutdown, stateChangeShutdownToAvailable })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);

            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(3240d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(standardLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Sku);

            Assert.Equal(2, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3420d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(standardLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Sku);
            Assert.Equal(180d, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Usage);
            Assert.Equal(premiumLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Sku);

            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentSkuChangePremiumToStandardToPremiumWhileShutdown()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = premiumLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var myPremiumEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = premiumLinuxSkuName }
            };

            var stateChangeAvailableToShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myPremiumEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };

            var skuChangeShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5).AddMinutes(3),
                Environment = myEnvironment,
                OldValue = "Shutdown",
                NewValue = "Shutdown"
            };

            var stateChangeShutdownToAvailable = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5).AddMinutes(6),
                Environment = myPremiumEnvironment,
                OldValue = "Shutdown",
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { stateChangeAvailableToShutdown, skuChangeShutdown, stateChangeShutdownToAvailable })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);

            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(3240d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(premiumLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Sku);

            Assert.Equal(2, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3420d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(premiumLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Sku);
            Assert.Equal(180d, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Usage);
            Assert.Equal(standardLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Sku);

            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentSkuChangeStandardToPremiumToStandardWhileArchived()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Archived",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var myPremiumEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = premiumLinuxSkuName }
            };

            var skuChangeToPremium = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myPremiumEnvironment,
                OldValue = "Archived",
                NewValue = "Archived"
            };

            var skuChangeToStandard = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5).AddMinutes(10),
                Environment = myEnvironment,
                OldValue = "Archived",
                NewValue = "Archived"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { skuChangeToPremium, skuChangeToStandard })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);

            Assert.Equal(2, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(standardLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Sku);
            Assert.Equal(3000d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(premiumLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Sku);
            Assert.Equal(600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Usage);

            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentSkuChangePremiumToStandardToPremiumWhileArchived()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Archived",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = premiumLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var myPremiumEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = premiumLinuxSkuName }
            };

            var skuChangeToStandard = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = myEnvironment,
                OldValue = "Archived",
                NewValue = "Archived"
            };

            var skuChangeToPremium = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5).AddMinutes(10),
                Environment = myPremiumEnvironment,
                OldValue = "Archived",
                NewValue = "Archived"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { skuChangeToStandard, skuChangeToPremium })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);

            Assert.Equal(2, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3000d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(premiumLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Sku);
            Assert.Equal(600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Usage);
            Assert.Equal(standardLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Sku);

            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentSkuChangeStandardToPremiumToStandardWithActivation()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var myPremiumEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = premiumLinuxSkuName }
            };

            var stateChangeStandardAvailableToShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddMinutes(10),
                Environment = myEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };

            var skuChangeToPremiumShutdownToAvailable = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddMinutes(20),
                Environment = myPremiumEnvironment,
                OldValue = "Shutdown",
                NewValue = "Available"
            };

            var stateChangePremiumAvailableToShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddMinutes(30),
                Environment = myPremiumEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };

            var skuChangeToStandardShutdownToAvailable = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddMinutes(40),
                Environment = myEnvironment,
                OldValue = "Shutdown",
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { stateChangeStandardAvailableToShutdown, skuChangeToPremiumShutdownToAvailable, stateChangePremiumAvailableToShutdown, skuChangeToStandardShutdownToAvailable })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);

            Assert.Equal(2, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(standardLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Sku);
            Assert.Equal(600d, returnedBill.UsageDetail[0].ResourceUsage.Compute[1].Usage);
            Assert.Equal(premiumLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Compute[1].Sku);

            Assert.Equal(2, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(2400d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(standardLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Sku);
            Assert.Equal(1200d, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Usage);
            Assert.Equal(premiumLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Sku);

            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithEnvironmentSkuChangePremiumToStandardToPremiumWithActivation()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "MyEnvironment",
                Sku = new Sku() { Name = premiumLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var myEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var myPremiumEnvironment = new EnvironmentBillingInfo()
            {
                Id = "MyEnvironment",
                Name = "MyEnvironment",
                Sku = new Sku() { Name = premiumLinuxSkuName }
            };

            var stateChangeStandardAvailableToShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddMinutes(10),
                Environment = myPremiumEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };

            var skuChangeToPremiumShutdownToAvailable = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddMinutes(20),
                Environment = myEnvironment,
                OldValue = "Shutdown",
                NewValue = "Available"
            };

            var stateChangePremiumAvailableToShutdown = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddMinutes(30),
                Environment = myEnvironment,
                OldValue = "Available",
                NewValue = "Shutdown"
            };

            var skuChangeToStandardShutdownToAvailable = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddMinutes(40),
                Environment = myPremiumEnvironment,
                OldValue = "Shutdown",
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns<BillSummary, IDiagnosticsLogger>((bill, logger) => Task.FromResult(returnedBill = bill));
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)new List<EnvironmentStateChange>()
                {
                    stateChangeStandardAvailableToShutdown,
                    skuChangeToPremiumShutdownToAvailable,
                    stateChangePremiumAvailableToShutdown,
                    skuChangeToStandardShutdownToAvailable
                }));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);

            Assert.Equal(2, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(premiumLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Sku);
            Assert.Equal(600d, returnedBill.UsageDetail[0].ResourceUsage.Compute[1].Usage);
            Assert.Equal(standardLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Compute[1].Sku);

            Assert.Equal(2, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(2400d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(premiumLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Sku);
            Assert.Equal(1200d, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Usage);
            Assert.Equal(standardLinuxSkuName, returnedBill.UsageDetail[0].ResourceUsage.Storage[1].Sku);

            Assert.Equal(1, returnedBill.UsageDetail.Count);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithTwoActiveEnvironments()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "EnvironmentOne",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            EnvironmentUsage twoActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "EnvironmentTwo",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive, twoActive },
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up an empty environment table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(Enumerable.Empty<EnvironmentStateChange>()));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(2, returnedBill.UsageDetail.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[1].ResourceUsage.Compute.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[1].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[1].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[1].ResourceUsage.Storage[0].Usage);

        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithTwoSuspendedEnvironments()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Shutdown",
                Id = "EnvironmentOne",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            EnvironmentUsage twoActive = new EnvironmentUsage()
            {
                EndState = "Shutdown",
                Id = "EnvironmentTwo",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive, twoActive },
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up an empty environment table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(Enumerable.Empty<EnvironmentStateChange>()));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(2, returnedBill.UsageDetail.Count);
            Assert.Equal(0, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(0, returnedBill.UsageDetail[1].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[1].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[1].ResourceUsage.Storage[0].Usage);

        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithOneSuspendedOneActiveEnvironment()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "EnvironmentOne",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            EnvironmentUsage twoActive = new EnvironmentUsage()
            {
                EndState = "Shutdown",
                Id = "EnvironmentTwo",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive, twoActive },
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up an empty environment table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult(Enumerable.Empty<EnvironmentStateChange>()));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(2, returnedBill.UsageDetail.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(0, returnedBill.UsageDetail[1].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[1].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[1].ResourceUsage.Storage[0].Usage);

        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_NoPreviousBillWithTwoActiveEnvironmentsCreated()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            var oneEnvironment = new EnvironmentBillingInfo()
            {
                Id = "EnvironmentOne",
                Name = "EnvironmentOne",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var environmentOneState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = oneEnvironment,
                OldValue = null,
                NewValue = "Available"
            };

            var twoEnvironment = new EnvironmentBillingInfo()
            {
                Id = "EnvironmentTwo",
                Name = "EnvironmentTwo",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var environmentTwoState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = twoEnvironment,
                OldValue = null,
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)null));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { environmentOneState, environmentTwoState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour.AddHours(1).AddDays(-2), returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(2, returnedBill.UsageDetail.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[1].ResourceUsage.Compute.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[1].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[1].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[1].ResourceUsage.Storage[0].Usage);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_NoPreviousBillWithTwoSuspendedEnvironmentsCreated()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            var oneEnvironment = new EnvironmentBillingInfo()
            {
                Id = "EnvironmentOne",
                Name = "EnvironmentOne",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var environmentOneState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = oneEnvironment,
                OldValue = null,
                NewValue = "Shutdown"
            };

            var twoEnvironment = new EnvironmentBillingInfo()
            {
                Id = "EnvironmentTwo",
                Name = "EnvironmentTwo",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var environmentTwoState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = twoEnvironment,
                OldValue = null,
                NewValue = "Shutdown"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)null));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { environmentOneState, environmentTwoState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour.AddHours(1).AddDays(-2), returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(2, returnedBill.UsageDetail.Count);
            Assert.Equal(0, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(0, returnedBill.UsageDetail[1].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[1].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[1].ResourceUsage.Storage[0].Usage);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_NoPreviousBillWithOneActiveOneSuspendedEnvironmentsCreated()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            var oneEnvironment = new EnvironmentBillingInfo()
            {
                Id = "EnvironmentOne",
                Name = "EnvironmentOne",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var environmentOneState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = oneEnvironment,
                OldValue = null,
                NewValue = "Available"
            };

            var twoEnvironment = new EnvironmentBillingInfo()
            {
                Id = "EnvironmentTwo",
                Name = "EnvironmentTwo",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var environmentTwoState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = twoEnvironment,
                OldValue = null,
                NewValue = "Shutdown"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)null));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up environment state change table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
                .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { environmentOneState, environmentTwoState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour.AddHours(1).AddDays(-2), returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(2, returnedBill.UsageDetail.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(0, returnedBill.UsageDetail[1].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[1].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[1].ResourceUsage.Storage[0].Usage);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithActiveEnvironmentActiveEnvironmentCreated()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "EnvironmentOne",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var twoEnvironment = new EnvironmentBillingInfo()
            {
                Id = "EnvironmentTwo",
                Name = "EnvironmentTwo",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var environmentTwoState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = twoEnvironment,
                OldValue = null,
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up an empty environment table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
               .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { environmentTwoState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(2, returnedBill.UsageDetail.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[1].ResourceUsage.Compute.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[1].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[1].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[1].ResourceUsage.Storage[0].Usage);

        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithSuspendedEnvironmentSuspendedEnvironmentCreated()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Shutdown",
                Id = "EnvironmentOne",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var twoEnvironment = new EnvironmentBillingInfo()
            {
                Id = "EnvironmentTwo",
                Name = "EnvironmentTwo",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var environmentTwoState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = twoEnvironment,
                OldValue = null,
                NewValue = "Shutdown"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up an empty environment table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
               .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { environmentTwoState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(2, returnedBill.UsageDetail.Count);
            Assert.Equal(0, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(0, returnedBill.UsageDetail[1].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[1].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[1].ResourceUsage.Storage[0].Usage);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithSuspendedEnvironmentActiveEnvironmentCreated()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Shutdown",
                Id = "EnvironmentOne",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var twoEnvironment = new EnvironmentBillingInfo()
            {
                Id = "EnvironmentTwo",
                Name = "EnvironmentTwo",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var environmentTwoState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = twoEnvironment,
                OldValue = null,
                NewValue = "Available"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up an empty environment table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
               .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { environmentTwoState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(2, returnedBill.UsageDetail.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(0, returnedBill.UsageDetail[1].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[1].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[1].ResourceUsage.Storage[0].Usage);
        }

        [Fact]
        public async Task GenerateBillingSummaryAsync_OnePreviousBillWithActiveEnvironmentSuspendedEnvironmentCreated()
        {
            var planId = "testPlanId";
            // Set up the data
            VsoPlanInfo planInfo = new VsoPlanInfo()
            {
                Name = "myPlan",
                Subscription = Guid.NewGuid().ToString(),
                ResourceGroup = "myRG",
                Location = AzureLocation.WestUs2,
            };

            var topOfCurrHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
            var billSummaryRequest = new BillingSummaryRequest()
            {
                BillingOverrides = Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>(),
                DesiredEndTime = topOfCurrHour.AddHours(1),
                PlanId = planId,
                PlanInformation = planInfo,
                Partner = Partner.GitHub,
            };

            EnvironmentUsage oneActive = new EnvironmentUsage()
            {
                EndState = "Available",
                Id = "EnvironmentOne",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var lastSummary = new BillSummary()
            {
                PeriodStart = topOfCurrHour.AddHours(-1),
                PeriodEnd = topOfCurrHour,
                UsageDetail = new List<EnvironmentUsage>() { oneActive },
            };

            var twoEnvironment = new EnvironmentBillingInfo()
            {
                Id = "EnvironmentTwo",
                Name = "EnvironmentTwo",
                Sku = new Sku() { Name = standardLinuxSkuName }
            };

            var environmentTwoState = new EnvironmentStateChange()
            {
                Time = topOfCurrHour.AddHours(0.5),
                Environment = twoEnvironment,
                OldValue = null,
                NewValue = "Shutdown"
            };

            var usage = new Dictionary<string, double>
            {
                { "myMeter", 1 }
            };

            // Set up the mocks            
            // Set up the bill manager.
            BillSummary returnedBill = null;
            BillSummaryManager.Setup(x => x.GetLatestBillSummaryAsync(planId, It.IsAny<IDiagnosticsLogger>())).Returns(Task.FromResult((BillSummary)lastSummary));
            BillSummaryManager.Setup(x => x.CreateOrUpdateAsync(It.IsAny<BillSummary>(), It.IsAny<IDiagnosticsLogger>())).Callback<BillSummary, IDiagnosticsLogger>((bill, logger) => returnedBill = bill);
            // Set up an empty environment table
            EnvironmentStateChangeManager.Setup(x => x.GetAllRecentEnvironmentEvents(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>()))
               .Returns(Task.FromResult((IEnumerable<EnvironmentStateChange>)(new List<EnvironmentStateChange>() { environmentTwoState })));
            // Set up the Billing meter Service
            BillingMeterService.Setup(x => x.GetUsageBasedOnResources(It.IsAny<ResourceUsageDetail>(), planInfo, It.IsAny<DateTime>(), It.IsAny<IDiagnosticsLogger>(), Partner.GitHub)).Returns(usage);
            // Set up the PA billing storage 
            var storageClient = new Mock<IBillingSubmissionCloudStorageClient>();

            BillingStorageFactory.Setup(x => x.CreateBillingSubmissionCloudStorage(It.IsAny<AzureLocation>())).Returns(Task.FromResult(storageClient.Object));
            storageClient.Setup(x => x.InsertOrUpdateBillingTableSubmission(It.IsAny<BillingSummaryTableSubmission>())).Returns(Task.FromResult((BillingSummaryTableSubmission)null));
            storageClient.Setup(x => x.PushBillingQueueSubmission(It.IsAny<BillingSummaryQueueSubmission>())).Returns(Task.CompletedTask);
            // execute the test
            await BillSummaryGenerator.GenerateBillingSummaryAsync(billSummaryRequest, Logger);

            //verify the bill
            Assert.Equal(topOfCurrHour.AddHours(1), returnedBill.PeriodEnd);
            Assert.Equal(topOfCurrHour, returnedBill.PeriodStart);
            Assert.Equal(1, returnedBill.Usage.Count);
            Assert.Equal(2, returnedBill.UsageDetail.Count);
            Assert.Equal(0, returnedBill.UsageDetail[0].ResourceUsage.Compute.Count);
            Assert.Equal(1, returnedBill.UsageDetail[0].ResourceUsage.Storage.Count);
            Assert.Equal(1800d, returnedBill.UsageDetail[0].ResourceUsage.Storage[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[1].ResourceUsage.Compute.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[1].ResourceUsage.Compute[0].Usage);
            Assert.Equal(1, returnedBill.UsageDetail[1].ResourceUsage.Storage.Count);
            Assert.Equal(3600d, returnedBill.UsageDetail[1].ResourceUsage.Storage[0].Usage);

        }

        protected Mock<ISkuCatalog> GetMockSKuCatalog()
        {
            var mockStandardLinux = new Mock<ICloudEnvironmentSku>();
            mockStandardLinux.Setup(sku => sku.StorageSizeInGB).Returns(standadLinuxStorageSizeInGB);
            mockStandardLinux.Setup(sku => sku.ComputeVsoUnitsPerHour).Returns(standardLinuxComputeUnitPerHr);
            mockStandardLinux.Setup(sku => sku.StorageVsoUnitsPerHour).Returns(standardLinuxStorageUnitPerHr);

            var mockPremiumLinux = new Mock<ICloudEnvironmentSku>();
            mockPremiumLinux.Setup(sku => sku.StorageSizeInGB).Returns(premiumLinuxStorageSizeInGB);
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
