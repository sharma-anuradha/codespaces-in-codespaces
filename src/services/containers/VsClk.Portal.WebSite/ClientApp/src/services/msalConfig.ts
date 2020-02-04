import * as msal from '@vs/msal';
import { aadAuthorityUrlCommon } from '../constants';

import { storageAdapter } from './StorageAdapter';

export const msalConfig: msal.Configuration = {
    auth: {
        clientId: 'a3037261-2c94-4a2e-b53f-090f6cdd712a',
        authority: aadAuthorityUrlCommon,
        validateAuthority: false,
        navigateToLoginRequestUrl: false,
        redirectUri: location.origin,
    },
    cache: {
        cacheLocation: storageAdapter
    },
};

export let clientApplication: msal.UserAgentApplication | null = null;

export const initializeMsal = () => {
    clientApplication = new msal.UserAgentApplication(msalConfig);
}