import { expirationTimeBackgroundTokenRefreshThreshold } from '../constants';
import { randomStr } from '../utils/randomStr';
import { Signal } from '../utils/signal';
import { createTrace } from '../utils/createTrace';
import { parseJWTToken } from '../utils/parseJWTToken';
import { getTokenExpiration } from '../utils/getTokenExpiration';

import { createNavigateUrl } from './msal/createNavigateUrl';
import { ensureRedirectionIframe } from './msal/ensureRedirectionIframe';

import { inLocalStorageJWTTokenCacheFactory } from '../cache/localstorageJWTCache';

import { IToken } from '../typings/IToken';

const ARM_CACHE_TOKEN_NAME = 'vso-arm-token';
const IFRAME_REDIRECT_POLL_INTERVAL = 50;

const trace = createTrace('AuthARMService');

const tokenCache = inLocalStorageJWTTokenCacheFactory();

export const refreshArmToken = (renewUrl: URL, nonce: string, timeout: number = 10000): Promise<IToken | null> => {
    const clearTimers = () => {
        clearInterval(intervalHandle);
        clearTimeout(timeoutHandle);
    }

    const signal = new Signal<IToken | null>();

    const timeoutHandle = setTimeout(() => {
        signal.reject(new Error('No access token found.'));
        clearInterval(intervalHandle);
    }, timeout);

    const iframe = ensureRedirectionIframe();
    const intervalHandle = setInterval(() => {
        if (!iframe.contentWindow) {
            // the iframe has different domain hence didn't redirect back yet
            return;
        }
        let { href, hash } = iframe.contentWindow.location;
        if (href && hash) {
            const searchParams = new URLSearchParams(hash.replace('#', ''));

            const error = searchParams.get('error');

            if (error) {
                const errorDescription = searchParams.get('error_description') || error || 'Redirection error.';
                trace.error(errorDescription);

                clearTimers();
                signal.complete(null);
                return;
            }

            const state = searchParams.get('state');

            if (state !== nonce) {
                trace.error('State params do not match.');
                signal.complete(null);
                return;
            }

            const accessToken = searchParams.get('access_token');
            
            if (!accessToken) {
                clearTimers();
                trace.error('No access token found.');
                signal.complete(null);

                return;
            }

            clearTimers();

            const token = parseJWTToken(accessToken);
            tokenCache.cacheToken(ARM_CACHE_TOKEN_NAME, token);
            signal.complete(token);
        }
    }, IFRAME_REDIRECT_POLL_INTERVAL);

    iframe.src = `about:blank`;
    iframe.src = renewUrl.toString();

    return signal.promise;
}

export const getFreshArmToken = async (timeout: number = 10000) => {
    // get the precalculated redirection URL from MSAL.js
    const nonce = randomStr();
    const renewUrl = await createNavigateUrl(nonce);

    const token = await refreshArmToken(renewUrl, nonce, timeout);

    return token;
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

export const signOut = () => {
    tokenCache.clearCache();
}