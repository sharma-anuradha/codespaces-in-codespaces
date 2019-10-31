import { AuthResponse } from 'msal';

import { clientApplication, msalConfig } from './msalConfig';
import { autServiceTrace } from "./autServiceTrace";
import { tokenFromTokenResponse } from "./tokenFromTokenResponse";

import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';

export async function acquireToken(scopes: string[]): Promise<ITokenWithMsalAccount> {
    const tokenRequest = {
        scopes,
        authority: msalConfig.auth.authority,
    };
    let tokenResponse: AuthResponse;
    try {
        tokenResponse = await clientApplication.acquireTokenSilent(tokenRequest);
    }
    catch (err) {
        autServiceTrace.warn(err);
        if (err.name === 'InteractionRequiredAuthError') {
            tokenResponse = await clientApplication.acquireTokenPopup(tokenRequest);
        }
        else {
            autServiceTrace.error(err);
            throw err;
        }
    }
    return tokenFromTokenResponse(tokenResponse);
}
