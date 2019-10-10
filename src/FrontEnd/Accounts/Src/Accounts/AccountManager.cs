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
        /// Creates or Updates an account.
        /// </summary>
        public async Task<VsoAccount> CreateOrUpdateAsync(VsoAccount model, IDiagnosticsLogger logger)
        {
            var savedModel = await GetAsync(model.Account, logger);
            if (savedModel != null)
            {
                var plan = model.Plan;
                if (savedModel.Plan?.Name != plan?.Name)
                {
                    savedModel.Plan = plan;
                }

                return await this.accountRepository.CreateOrUpdateAsync(savedModel, logger);
            }

            model.Id = Guid.NewGuid().ToString();
            return await this.accountRepository.CreateOrUpdateAsync(model, logger);
        }

        /// <summary>
        /// Retrieves an existing account using the provided account info.
        /// </summary>
        public async Task<VsoAccount> GetAsync(VsoAccountInfo account, IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(account, nameof(VsoAccountInfo));

            var results = await this.accountRepository.GetWhereAsync((model) => model.Account == account, logger, null);
            return results.SingleOrDefault();
        }

        /// <summary>
        /// Retrieves an enumerable list of accounts in a subscription.
        /// </summary>
        /// <param name="userId">ID of the owner of the accounts to list, or null
        /// to list accounts owned by any user.</param>
        /// <param name="subscriptionId">ID of the subscription containing the accounts, or null
        /// to list accounts across all a user's subscriptions. Required if userId is omitted.</param>
        /// <param name="resourceGroup">Optional name of the resource group containing the accounts,
        /// or null to list accounts across all resource groups in the subscription.</param>
        public async Task<IEnumerable<VsoAccount>> ListAsync(
            string userId,
            string subscriptionId,
            string resourceGroup,
            IDiagnosticsLogger logger)
        {
            if (userId != null)
            {
                if (resourceGroup != null)
                {
                    ValidationUtil.IsRequired(subscriptionId, nameof(subscriptionId));

                    return await this.accountRepository.GetWhereAsync(
                        (model) => model.UserId == userId &&
                            model.Account.Subscription == subscriptionId &&
                            model.Account.ResourceGroup == resourceGroup,
                        logger,
                        null);
                }
                else if (subscriptionId != null)
                {
                    return await this.accountRepository.GetWhereAsync(
                        (model) => model.UserId == userId &&
                            model.Account.Subscription == subscriptionId,
                        logger,
                        null);
                }
                else
                {
                    return await this.accountRepository.GetWhereAsync(
                        (model) => model.UserId == userId, logger, null);
                }
            }
            else
            {
                ValidationUtil.IsRequired(subscriptionId, nameof(subscriptionId));

                if (resourceGroup != null)
                {
                    return await this.accountRepository.GetWhereAsync(
                        (model) => model.Account.Subscription == subscriptionId &&
                            model.Account.ResourceGroup == resourceGroup,
                        logger,
                        null);
                }
                else
                {
                    return await this.accountRepository.GetWhereAsync(
                        (model) => model.Account.Subscription == subscriptionId, logger, null);
                }
            }
        }

        /// <summary>
        /// Deletes an exisitng account using the provided account info.
        /// </summary>
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
