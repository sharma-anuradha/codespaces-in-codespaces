export interface IAzureSubscriptionPolicy {
    locationPlacementId: string;
    quotaId: string;
    spendingLimit: string;
}

export interface IAzureSubscription {
    id: string;
    authorizationSource: string;
    displayName: string;
    state: string;
    subscriptionId: string;
    subscriptionPolicies: IAzureSubscriptionPolicy;
    tenantId: string;
}
