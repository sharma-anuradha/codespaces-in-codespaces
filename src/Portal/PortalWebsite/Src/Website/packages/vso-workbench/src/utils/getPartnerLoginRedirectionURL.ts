import { IPartnerInfo, isGithubTLD } from 'vso-client-core';
import { VSCodespacesPlatformInfoGeneral } from 'vs-codespaces-authorization';

const getFallbackPartnerRedirectionUrl = (): URL | null => {
    if (isGithubTLD(location.href)) {
        return new URL('https://github.com/codespaces/auth');
    }

    // fallback to salesforce portal
    return new URL('https://login.salesforce.com/lightning/n/CodeBuilder__Workspace_Manager');
};

export const getPartnerLoginRedirectionURL = (
    info: IPartnerInfo | VSCodespacesPlatformInfoGeneral | null
): URL | null => {
    if (!info) {
        return getFallbackPartnerRedirectionUrl();
    }

    const { managementPortalUrl } = info;
    if (!managementPortalUrl) {
        return null;
    }

    return new URL(managementPortalUrl);
};
