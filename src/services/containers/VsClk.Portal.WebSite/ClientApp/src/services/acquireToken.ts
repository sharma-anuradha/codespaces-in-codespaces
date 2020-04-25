import { clientApplication, msalConfig } from './msalConfig';
import { autServiceTrace } from "./autServiceTrace";
import { tokenFromTokenResponse } from "./tokenFromTokenResponse";

import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';

import { signal2FARequired } from '../actions/login';

export async function acquireTokenSilent(scopes: string[]): Promise<ITokenWithMsalAccount | undefined> {
    const tokenRequest = {
        scopes,
        authority: msalConfig.auth.authority,
    };
    try {
        if (!clientApplication) {
            throw new Error('Initialize MSAL client application first.');
        }

        const tokenResponse = await clientApplication.acquireTokenSilent(tokenRequest);

        return tokenFromTokenResponse(tokenResponse);
    }
    catch (err) {
        autServiceTrace.error('Acquire token silent:', err);
    }
}

export async function acquireToken(scopes: string[]): Promise<ITokenWithMsalAccount | null> {
    const tokenRequest = {
        scopes,
        authority: msalConfig.auth.authority,
    };

    if (!clientApplication) {
        throw new Error('Initialize MSAL client application first.');
    }

    try {
        const tokenResponse = await clientApplication.acquireTokenSilent(tokenRequest);
        return tokenFromTokenResponse(tokenResponse);
    }
    catch (err) {
        if (err.name === 'InteractionRequiredAuthError') {
            signal2FARequired();
            return null;
        }

        autServiceTrace.error(err);
        throw err;
    }

    return null;
}
