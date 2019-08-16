using Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore;
using Microsoft.VsCloudKernel.Services.EnvReg.Repositories;
using Microsoft.VsSaaS.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using VsClk.EnvReg.Models.Errors;

namespace VsClk.EnvReg.Repositories
{
    public class AccountManager : IAccountManager
    {
        private IBillingAccountRepository BillingAccountRepository { get; }

        public AccountManager(IBillingAccountRepository billingAccountRepository)
        {
            BillingAccountRepository = billingAccountRepository;
        }

        /// <summary>
        /// Creates or Updates a BillingAccount.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public async Task<BillingAccount> CreateOrUpdateAsync(BillingAccount model, IDiagnosticsLogger logger)
        {
            var modelList = await GetAsync(model.Account, logger);
            var savedModel = modelList.ToList().SingleOrDefault();

            if (savedModel != null)
            {
                var plan = model.Plan;
                if (savedModel.Plan.Name != plan.Name) { savedModel.Plan = plan; }

                return await BillingAccountRepository.CreateOrUpdateAsync(savedModel, logger);
            }

            model.Id = Guid.NewGuid().ToString();
            return await BillingAccountRepository.CreateOrUpdateAsync(model, logger);
        }

        /// <summary>
        /// Retrieves an existing BillingAccount using the provided BillingAccountInfo.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public async Task<IEnumerable<BillingAccount>> GetAsync(BillingAccountInfo account, IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(account, nameof(BillingAccountInfo));

            return await BillingAccountRepository.GetWhereAsync((model) => model.Account == account, logger, null);
        }

        /// <summary>
        /// Retrieves an enumerable list of BillingAccounts using the provided BillingAccountInfo .
        /// </summary>
        /// <param name="account"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public async Task<IEnumerable<BillingAccount>> GetListAsync(string subscriptionId, string resourceGroup, IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(subscriptionId);
            ValidationUtil.IsRequired(resourceGroup);

            return await BillingAccountRepository.GetWhereAsync((model)=> model.Account.Subscription == subscriptionId &&
                                                                          model.Account.ResourceGroup == resourceGroup, logger, null);
        }

        public async Task<IEnumerable<BillingAccount>> GetListBySubscriptionAsync(string subscriptionId, IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(subscriptionId);

            return await BillingAccountRepository.GetWhereAsync((model) => model.Account.Subscription == subscriptionId, logger, null);
        }

        /// <summary>
        /// Deletes an exisitng BillingAccount using the provided BillingAccountInfo.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(BillingAccountInfo account, IDiagnosticsLogger logger)
        {
            //Find model in DB
            //Location is not provided on a DELETE operation from RPSaaS, 
            //thus we can only compare Name, Subscription, and ResourceGroup which should be sufficient
            var savedModel = await BillingAccountRepository.GetWhereAsync((model) => model.Account.Name == account.Name &&
                                                                                     model.Account.Subscription == account.Subscription &&
                                                                                     model.Account.ResourceGroup == account.ResourceGroup, logger, null);
            var modelList = savedModel.ToList().SingleOrDefault();

            if (modelList == null)
            {
                //Nothing to delete, Account does not exist
                return false;
            }

           return await BillingAccountRepository.DeleteAsync(modelList.Id, logger);
        }
    }
}
