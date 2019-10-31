import * as msal from 'msal';
import { aadAuthorityUrlCommon } from '../constants';

export const msalConfig: msal.Configuration = {
    auth: {
        clientId: 'a3037261-2c94-4a2e-b53f-090f6cdd712a',
        authority: aadAuthorityUrlCommon,
        validateAuthority: false,
        navigateToLoginRequestUrl: false,
        redirectUri: location.origin,
    },
    cache: {
        cacheLocation: 'localStorage',
        storeAuthStateInCookie: true,
    },
};

export const clientApplication = new msal.UserAgentApplication(msalConfig);
