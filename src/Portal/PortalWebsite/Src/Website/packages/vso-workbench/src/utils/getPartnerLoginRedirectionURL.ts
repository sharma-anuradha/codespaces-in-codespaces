import { IPartnerInfo } from 'vso-client-core';
import { VSCodespacesPlatformInfoGeneral } from 'vs-codespaces-authorization';

const getFallbackSalesforceRedirectionUrl = (): URL | null => {
    return new URL('https://login.salesforce.com/lightning/n/CodeBuilder__Workspace_Manager');
};

const getFallbackPartnerRedirectionUrl = (): URL | null => {
    // fallback to salesforce portal
    return getFallbackSalesforceRedirectionUrl();
};

export const getPartnerLoginRedirectionURL = (info: IPartnerInfo | VSCodespacesPlatformInfoGeneral | null): URL | null => {
    if (!info) {
        return getFallbackPartnerRedirectionUrl();
    }

    const { managementPortalUrl } = info;
    if (!managementPortalUrl) {
        return null;
    }

    return new URL(managementPortalUrl);
};
