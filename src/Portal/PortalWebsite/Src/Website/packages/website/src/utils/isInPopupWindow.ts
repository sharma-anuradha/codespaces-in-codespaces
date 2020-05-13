export const isInPopupWindow = (): boolean => {
    if (
        window.location.href.toLowerCase().includes('github/login') ||
        window.location.href.toLowerCase().includes('azdev/login')
    ) {
        // Both GitHub and AzureDevOps are technically not popups since they are opened in new tab,
        // but currently we cannot differentiate between a popup and tabs.
        return false;
    }

    if (!window.opener) {
        return false;
    }

    try {
        // Getting origin of cross-site opener throws.
        const openerOrigin = window.opener.origin;
        if (openerOrigin && window.opener !== window) {
            return true;
        }

        return false;
    } catch (ex) {
        return false;
    }
};
