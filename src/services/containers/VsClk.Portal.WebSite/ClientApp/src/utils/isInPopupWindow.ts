export const isInPopupWindow = (): boolean => {
    if (
        window.location.href.toLowerCase().includes('github/login') ||
        window.location.href.toLowerCase().includes('azdev/login')
    ) {
        // Both GitHub and AzureDevOps are technically not popups since they are opened in new tab,
        // but currently we cannot differentiate between a popup and tabs.
        return false;
    }
    return !!(window.opener && window.opener !== window);
};
