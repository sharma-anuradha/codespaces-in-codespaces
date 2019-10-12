import { expirationTimeBackgroundTokenRefreshThreshold } from '../constants';
import { randomStr } from '../utils/randomStr';
import { createTrace } from '../utils/createTrace';
import { getTokenExpiration } from '../utils/getTokenExpiration';
import { createNavigateUrl } from './msal/createNavigateUrl';
import { inLocalStorageJWTTokenCacheFactory } from '../cache/localstorageJWTCache';
import { LoginRequiredError } from './msal/renewTokenFactory';
import { getFreshArmTokenSilentFactory } from './msal/getFreshArmTokenSilent';
import { getFreshArmTokenPopup } from './msal/getFreshArmTokenPopup';

import { IToken } from '../typings/IToken';

const ARM_CACHE_TOKEN_NAME = 'vso-arm-token';
const tokenCache = inLocalStorageJWTTokenCacheFactory();

export const trace = createTrace('AuthARMService');

export const getFreshArmToken = async (timeout: number = 10000) => {
    // get the precalculated redirection URL from MSAL.js
    const nonce = randomStr();
    const renewUrl = await createNavigateUrl(nonce);
    try {
        const getFreshArmTokenSilent = getFreshArmTokenSilentFactory();
        
        const token = await getFreshArmTokenSilent(renewUrl, nonce, timeout);
        if (token) {
            tokenCache.cacheToken(ARM_CACHE_TOKEN_NAME, token);
        }
        return token;
    } catch (e) {
        // if login is required, popup window for the user
        if (e instanceof LoginRequiredError) {
            const token = await getFreshArmTokenPopup(renewUrl, nonce, timeout);
            if (token) {
                tokenCache.cacheToken(ARM_CACHE_TOKEN_NAME, token);
            }
            return token;
        }
    }
    return null;
}

export const getARMToken = async (expiration: number, timeout: number = 10000): Promise<IToken | null> => {
    const cachedToken = tokenCache.getCachedToken(ARM_CACHE_TOKEN_NAME, expiration);
    if (cachedToken) {
        const expirationTime = getTokenExpiration(cachedToken);
        if (expirationTime <= expirationTimeBackgroundTokenRefreshThreshold) {
            getFreshArmToken(timeout);
        }
        return cachedToken;
    }
    return getFreshArmToken(timeout);
}
export const logout = () => {
    tokenCache.clearCache();
}
