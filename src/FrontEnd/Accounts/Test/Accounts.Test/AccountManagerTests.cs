using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Xunit;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Accounts.Tests
{
    public class AccountManagerTests
    {
        private readonly IAccountRepository accountRepository;
        private readonly AccountManager accountManager;
        private readonly IDiagnosticsLoggerFactory loggerFactory;
        private readonly IDiagnosticsLogger logger;
        private static readonly string subscription = Guid.NewGuid().ToString();
        
        public AccountManagerTests()
        {
            loggerFactory = new DefaultLoggerFactory();
            logger = loggerFactory.New();

            this.accountRepository = new MockAccountRepository();
            this.accountManager = new AccountManager(this.accountRepository);
        }

        private VsoAccount GenerateAccount(string name, string subscriptionOption = null)
        {
            return new VsoAccount
            {
                Account = new VsoAccountInfo
                {
                    Subscription = subscriptionOption ?? subscription,
                    ResourceGroup = "myRG",
                    Name = name,
                    Location = AzureLocation.WestUs2,
                },
                Plan = new Sku
                {
                    Name = "Preview"
                }
            };  
        }

        [Fact]
        public async Task CreateAccount()
        {
            var savedModel = await accountManager.CreateOrUpdateAsync(GenerateAccount("CreateAccountTest"), logger);
            Assert.NotNull(savedModel);
            Assert.NotNull(savedModel.Id);
        }

        [Fact]
        public async Task GetAccount()
        {
            var original = await accountManager.CreateOrUpdateAsync(GenerateAccount("GetAccountTest"), logger);
            var savedModelList = await accountManager.GetAsync(original.Account, logger);
            var savedModel = savedModelList.ToList().FirstOrDefault();
            Assert.NotNull(savedModel);
            Assert.NotNull(savedModel.Id);
            Assert.Equal(original.Account, savedModel.Account);
        }

        [Fact]
        public async Task UpdateAccount()
        {
            var original = await accountManager.CreateOrUpdateAsync(GenerateAccount("UpdateAccountTest"), logger);
            var savedModelList = await accountManager.GetAsync(original.Account, logger);
            var savedModel = savedModelList.ToList().FirstOrDefault();
            savedModel.Plan = new Sku { Name = "Private" };
            var updatedModel = await accountManager.CreateOrUpdateAsync(savedModel, logger);
            Assert.NotNull(updatedModel);
            Assert.Equal(savedModel.Account, updatedModel.Account);
            Assert.Equal("Private", updatedModel.Plan.Name);
        }

        [Fact]
        public async Task DeleteAccount()
        {
            var savedModel = await accountManager.CreateOrUpdateAsync(GenerateAccount("DeleteAccountTest"), logger);
            var result = await accountManager.DeleteAsync(savedModel.Account, logger);
            Assert.True(result);

            var deletedList = await accountManager.GetAsync(savedModel.Account, logger);
            var deleted = deletedList.FirstOrDefault();
            Assert.Null(deleted);
        }

        [Fact]
        public async Task GetAllAccountsBySubscriptionAndRG()
        {
            var model1 = GenerateAccount("Model1");
            await accountManager.CreateOrUpdateAsync(model1, logger);
            await accountManager.CreateOrUpdateAsync(GenerateAccount("Model2"), logger);
            await accountManager.CreateOrUpdateAsync(GenerateAccount("Model3"), logger);

            var modelList = await accountManager.GetListAsync(model1.Account.Subscription, model1.Account.ResourceGroup, logger);
            Assert.NotNull(modelList);
            Assert.IsAssignableFrom<IEnumerable>(modelList);
            Assert.All(modelList, item => Assert.Contains(model1.Account.Subscription, model1.Account.Subscription));
        }

        [Fact]
        public async Task GetAllAccountsBySubscription()
        {
            var subscriptionGuid1 = Guid.NewGuid().ToString();
            var subscriptionGuid2 = Guid.NewGuid().ToString();
            var model1 = GenerateAccount("Model1", subscriptionGuid1);
            await accountManager.CreateOrUpdateAsync(model1, logger);
            await accountManager.CreateOrUpdateAsync(GenerateAccount("Model2", subscriptionGuid2), logger);
            await accountManager.CreateOrUpdateAsync(GenerateAccount("Model3", subscriptionGuid2), logger);

            var modelListFirst = await accountManager.GetListBySubscriptionAsync(subscriptionGuid1, logger);
            var listFirst = modelListFirst.ToList();
            Assert.NotNull(listFirst);
            Assert.Single(listFirst);

            var modelListSecond = await accountManager.GetListBySubscriptionAsync(subscriptionGuid2, logger);
            var listSecond = modelListSecond.ToList();
            Assert.NotNull(listSecond);
            Assert.Equal(2, listSecond.Count());
        }
    }
}
