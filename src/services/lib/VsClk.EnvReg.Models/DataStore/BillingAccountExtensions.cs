namespace Microsoft.VsCloudKernel.Services.EnvReg.Models.DataStore
{
    public static class BillingAccountExtensions
    {
        // TODO: Update these identifiers.
        public const string ProviderName = "Microsoft.VSOnline";
        public const string AccountResourceType = "vsonlineaccount";
        public const string ApiVersion = "2019-07";

        /// <summary>
        /// Gets the fully-qualified Azure resource path of the account, starting with "/subscriptions/...",
        /// and including the api-version query parameter.
        /// </summary>
        public static string GetResourcePath(this BillingAccount account) => account.Account?.GetResourcePath();

        /// <summary>
        /// Gets the fully-qualified Azure resource path of the account, starting with "/subscriptions/...",
        /// and including the api-version query parameter.
        /// </summary>
        public static string GetResourcePath(this BillingAccountInfo account)
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
