import { expirationTimeBackgroundTokenRefreshThreshold, armAPIVersion } from '../constants';
import { randomStr } from '../utils/randomStr';
import { createTrace } from '../utils/createTrace';
import { getTokenExpiration } from '../utils/getTokenExpiration';
import { createNavigateUrl } from './msal/createNavigateUrl';
import { inLocalStorageJWTTokenCacheFactory } from '../cache/localstorageJWTCache';
import { LoginRequiredError } from './msal/renewTokenFactory';
import { getFreshArmTokenSilentFactory } from './msal/getFreshArmTokenSilent';
import { getFreshArmTokenPopup } from './msal/getFreshArmTokenPopup';

import { IToken } from '../typings/IToken';

const tokenCache = inLocalStorageJWTTokenCacheFactory('vso-arm-token');

export const trace = createTrace('AuthARMService');

export const getFreshArmTokenForTenant = async (tenantId: string, timeout: number = 10000) => {
    // get the precalculated redirection URL from MSAL.js
    const nonce = randomStr();
    const renewUrl = await createNavigateUrl(tenantId, nonce);
    try {
        const getFreshArmTokenSilent = getFreshArmTokenSilentFactory();
        
        const token = await getFreshArmTokenSilent(renewUrl, nonce, timeout);
        if (token) {
            tokenCache.cacheToken(tenantId, token);
        }
        return token;
    } catch (e) {
        // if login is required, popup window for the user
        if (e instanceof LoginRequiredError) {
            const token = await getFreshArmTokenPopup(renewUrl, nonce, timeout);
            if (token) {
                tokenCache.cacheToken(tenantId, token);
            }
            return token;
        }
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

export const getARMTokenForTenant = async (tenantId: string, expiration: number, timeout: number = 10000): Promise<IToken | null> => {
    const cachedToken = tokenCache.getCachedToken(tenantId, expiration);
    if (cachedToken) {
        const expirationTime = getTokenExpiration(cachedToken);
        if (expirationTime > expiration) {
            if (expirationTime <= expirationTimeBackgroundTokenRefreshThreshold) {
                getFreshArmTokenForTenant(tenantId, timeout);
            }
            return cachedToken;
        }
    }
    
    return await getFreshArmTokenForTenant(tenantId, timeout);
}

export const getARMToken = async (expiration: number, timeout: number = 10000): Promise<IToken | null> => {
    const armOrgsToken = await getARMTokenForTenant('organizations', expiration, timeout);

    if (!armOrgsToken) {
        return null;
    }

    const tenantId = await getTenantId(armOrgsToken);

    if (!tenantId) {
        return null;
    }

    const armToken = await getARMTokenForTenant(tenantId, expiration, timeout);

    return armToken;
}

export const logout = () => {
    tokenCache.clearCache();
}
