// <copyright file="VsoAccountExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Accounts
{
    public static class VsoAccountExtensions
    {
        public const string ProviderName = "Microsoft.VSOnline";
        public const string AccountResourceType = "accounts";
        public const string ApiVersion = "2019-07-01";

        /// <summary>
        /// Gets the fully-qualified Azure resource path of the account, starting with "/subscriptions/...",
        /// and including the api-version query parameter.
        /// </summary>
        public static string GetResourcePath(this VsoAccount account) => account.Account?.GetResourcePath();

        /// <summary>
        /// Gets the fully-qualified Azure resource path of the account, starting with "/subscriptions/...",
        /// and including the api-version query parameter.
        /// </summary>
        public static string GetResourcePath(this VsoAccountInfo account)
        {
            Requires.NotNull(account, nameof(account));
            Requires.NotNullOrWhiteSpace(account.Subscription, nameof(account.Subscription));
            Requires.NotNullOrWhiteSpace(account.ResourceGroup, nameof(account.ResourceGroup));
            Requires.NotNullOrWhiteSpace(account.Name, nameof(account.Name));

            return $"/subscriptions/{account.Subscription}/resourceGroups/{account.ResourceGroup}" +
                $"/providers/{ProviderName}/{AccountResourceType}/{account.Name}?api-version={ApiVersion}";
        }
    }
}
