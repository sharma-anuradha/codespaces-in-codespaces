import { randomString, createTrace } from 'vso-client-core';

import { expirationTimeBackgroundTokenRefreshThreshold, armAPIVersion } from '../constants';
import { getTokenExpiration } from '../utils/getTokenExpiration';
import { createNavigateUrl } from './msal/createNavigateUrl';
import { inLocalStorageJWTTokenCacheFactory } from '../cache/localstorageJWTCache';
import { LoginRequiredError } from './msal/renewTokenFactory';
import { getFreshArmTokenSilentFactory } from './msal/getFreshArmTokenSilent';
import { getFreshArmTokenPopup } from './msal/getFreshArmTokenPopup';

import { IToken } from '../typings/IToken';
import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';
import { parseJWTToken } from '../utils/parseJWTToken';

const tokenCache = inLocalStorageJWTTokenCacheFactory('vsonline.arm.token');

export const trace = createTrace('AuthARMService');

export const getFreshArmTokenForTenant = async (currentToken: ITokenWithMsalAccount, tenantId: string, timeout: number = 10000) => {
    // get the precalculated redirection URL from MSAL.js
    const nonce = randomString();

    if (!currentToken) {
        throw new Error('User is not authenticated.');
    }

    const renewUrl = await createNavigateUrl(currentToken, tenantId, nonce);
    try {
        const getFreshArmTokenSilent = getFreshArmTokenSilentFactory('access_token', 'hash');
        
        const tokenString = await getFreshArmTokenSilent(renewUrl, nonce, timeout);

        if (!tokenString) {
            return null;
        }

        const token = parseJWTToken(tokenString);

        if (token) {
            await tokenCache.cacheToken(tenantId, token);
        }
        return token;
    } catch (e) {
        // if login is required, popup window for the user
        if (e instanceof LoginRequiredError) {
            const tokenString = await getFreshArmTokenPopup(renewUrl, nonce, timeout);
            if (!tokenString) {
                return null;
            }

            const token = parseJWTToken(tokenString);

            if (token) {
                await tokenCache.cacheToken(tenantId, token);
            }
            return token;
        }
    }
    return null;
}

export const getFreshArmAuthCodeForTenant = async (currentToken: ITokenWithMsalAccount, tenantId: string, timeout: number = 10000): Promise<string | null> => {
    // get the precalculated redirection URL from MSAL.js
    const nonce = randomString();

    if (!currentToken) {
        throw new Error('User is not authenticated.');
    }

    const renewUrl = await createNavigateUrl(currentToken, tenantId, nonce);

    try {
        const getFreshArmAuthCodeSilent = getFreshArmTokenSilentFactory('code', 'query');

        renewUrl.searchParams.set('response_type', 'code');

        renewUrl.pathname = '/common/oauth2/authorize';

        const tokenString = await getFreshArmAuthCodeSilent(renewUrl, nonce, timeout);


        return tokenString;
    } catch {
        // ignore
    }

    return null;
}

interface IAzureTenant {
    id: string;
    tenantId: string;
    countryCode: string;
    displayName: string;
    domains: string[];
}

const getTenantId = async (armToken: IToken): Promise<string | null> => {
    try {
        const tenantsResponse = await fetch(`https://management.azure.com/tenants?api-version=${armAPIVersion}`, {
            headers: new Headers({
                Authorization: `Bearer ${armToken.accessToken}`
            })
        });
        
        const tenants = (await tenantsResponse.json()).value as IAzureTenant[];

        return tenants[0].tenantId;
    } catch (e) {
        trace.error(e);

        return null;
    }
}

export const getARMTokenForTenant = async (token: ITokenWithMsalAccount, tenantId: string, expiration: number, timeout: number = 10000): Promise<IToken | null> => {
    const cachedToken = await tokenCache.getCachedToken(tenantId, expiration);
    if (cachedToken) {
        const expirationTime = getTokenExpiration(cachedToken);
        if (expirationTime > expiration) {
            if (expirationTime <= expirationTimeBackgroundTokenRefreshThreshold) {
                getFreshArmTokenForTenant(token, tenantId, timeout);
            }
            return cachedToken;
        }
    }
    
    return await getFreshArmTokenForTenant(token, tenantId, timeout);
}

export const getARMToken = async (token: ITokenWithMsalAccount, expiration: number, timeout: number = 10000): Promise<IToken | null> => {
    const armOrgsToken = await getARMTokenForTenant(token, 'organizations', expiration, timeout);

    if (!armOrgsToken) {
        return null;
    }

    const tenantId = await getTenantId(armOrgsToken);

    if (!tenantId) {
        return null;
    }

    const armToken = await getARMTokenForTenant(token, tenantId, expiration, timeout);

    return armToken;
}

export const logout = () => {
    tokenCache.clearCache();
}
