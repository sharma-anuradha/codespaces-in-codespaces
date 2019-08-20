// <copyright file="AccountManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Accounts
{
    public class AccountManager : IAccountManager
    {
        private readonly IAccountRepository accountRepository;

        public AccountManager(IAccountRepository accountRepository)
        {
            this.accountRepository = accountRepository;
        }

        /// <summary>
        /// Creates or Updates a BillingAccount.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public async Task<VsoAccount> CreateOrUpdateAsync(VsoAccount model, IDiagnosticsLogger logger)
        {
            var modelList = await GetAsync(model.Account, logger);
            var savedModel = modelList.ToList().SingleOrDefault();

            if (savedModel != null)
            {
                var plan = model.Plan;
                if (savedModel.Plan.Name != plan.Name)
                {
                    savedModel.Plan = plan;
                }

                return await this.accountRepository.CreateOrUpdateAsync(savedModel, logger);
            }

            model.Id = Guid.NewGuid().ToString();
            return await this.accountRepository.CreateOrUpdateAsync(model, logger);
        }

        /// <summary>
        /// Retrieves an existing BillingAccount using the provided BillingAccountInfo.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public async Task<IEnumerable<VsoAccount>> GetAsync(VsoAccountInfo account, IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(account, nameof(VsoAccountInfo));

            return await this.accountRepository.GetWhereAsync((model) => model.Account == account, logger, null);
        }

        /// <summary>
        /// Retrieves an enumerable list of BillingAccounts using the provided BillingAccountInfo .
        /// </summary>
        /// <param name="account"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public async Task<IEnumerable<VsoAccount>> GetListAsync(string subscriptionId, string resourceGroup, IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(subscriptionId);
            ValidationUtil.IsRequired(resourceGroup);

            return await this.accountRepository.GetWhereAsync(
                (model) => model.Account.Subscription == subscriptionId &&
                           model.Account.ResourceGroup == resourceGroup,
                logger,
                null);
        }

        public async Task<IEnumerable<VsoAccount>> GetListBySubscriptionAsync(string subscriptionId, IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(subscriptionId);

            return await this.accountRepository.GetWhereAsync((model) => model.Account.Subscription == subscriptionId, logger, null);
        }

        /// <summary>
        /// Deletes an exisitng BillingAccount using the provided BillingAccountInfo.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(VsoAccountInfo account, IDiagnosticsLogger logger)
        {
            // Find model in DB
            // Location is not provided on a DELETE operation from RPSaaS,
            // thus we can only compare Name, Subscription, and ResourceGroup which should be sufficient
            var savedModel = await this.accountRepository.GetWhereAsync(
                (model) => model.Account.Name == account.Name &&
                           model.Account.Subscription == account.Subscription &&
                           model.Account.ResourceGroup == account.ResourceGroup,
                logger,
                null);
            var modelList = savedModel.ToList().SingleOrDefault();

            if (modelList == null)
            {
                // Nothing to delete, Account does not exist
                return false;
            }

            return await this.accountRepository.DeleteAsync(modelList.Id, logger);
        }
    }
}
