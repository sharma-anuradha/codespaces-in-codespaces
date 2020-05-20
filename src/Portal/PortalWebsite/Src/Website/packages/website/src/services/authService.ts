import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';
import { inLocalStorageJWTTokenCacheFactory } from '../cache/localstorageJWTCache';
import { getTokenExpiration } from '../utils/getTokenExpiration';
import { expirationTimeBackgroundTokenRefreshThreshold } from '../constants';

import {
    logout as logoutFromArmAuthService,
    getARMToken,
    getFreshArmAuthCodeForTenant,
} from './authARMService';
import { getAuthTokenSuccessAction } from '../actions/getAuthTokenActions';
import { IToken } from '../typings/IToken';

import { sendTelemetry } from '../utils/telemetry';
import { clientApplication, initializeMsal, msalConfig } from './msalConfig';
import { acquireTokenSilentWith2FA, acquireTokenSilent } from './acquireToken';

import { autServiceTrace } from './autServiceTrace';
import {
    Signal,
    enhanceEncryptionKeys,
    createKeys,
    localStorageKeychain,
    setKeychainKeys,
    addRandomKey,
    debounceInterval,
    getRandomKey,
    randomBytes,
} from 'vso-client-core';
import { getUserFromMsalToken } from '../utils/getUserFromMsalToken';
import { AuthResponse } from '@vs/msal';

const SCOPES = ['email openid offline_access api://9db1d849-f699-4cfb-8160-64bed3335c72/All'];

const LOCAL_STORAGE_KEY = 'vsonline.default.account';

const tokenCache = inLocalStorageJWTTokenCacheFactory();

export const fetchKeychainKeys = async () => {
    try {
        const result = await fetch('/keychain-keys', {
            method: 'GET',
            credentials: 'include',
        });

        const keys = await result.json();

        return enhanceEncryptionKeys(keys);
    } catch (e) {
        // ignore
    }

    return null;
};

interface IAuthCode {
    code: string;
    redirectionUrl: string;
}

export class AuthService {
    private initializeSignal = new Signal();

    private readonly loginRequest = {
        scopes: SCOPES,
        authority: msalConfig.auth.authority,
        state: randomBytes(16).toString('base64'),
    };

    public async init() {
        const stateParam = new URLSearchParams(window.location.hash.replace('#', '?')).get('state');
        let buffer: Buffer | undefined;
        if (stateParam && stateParam.split('|').length === 2) {
            buffer = Buffer.from(stateParam.split('|')[1], 'base64');
            if (buffer.length !== 16) {
                buffer = undefined;
            }
        }
        addRandomKey(buffer);

        const keychainKeys = await fetchKeychainKeys();

        if (keychainKeys) {
            setKeychainKeys(keychainKeys);
        }
        this.loginRequest.state = getRandomKey().key.toString('base64');

        await initializeMsal();

        this.initializeSignal.complete(void 0);

        const token = await this.getCachedToken();

        if (!token) {
            return;
        }

        await this.getKeychainKeys();
        await localStorageKeychain.rehash(true);
        this.keepUserAuthenticated();
    }

    public async getKeychainKeys() {
        const token = await this.getCachedToken();

        if (!token) {
            throw new Error('Cannot get access token.');
        }

        const keys = await createKeys(token.accessToken);
        setKeychainKeys(keys);
    }

    public async login() {
        await this.initializeSignal.promise;

        if (!clientApplication) {
            throw new Error('Initialize MSAL client application first.');
        }

        clientApplication.loginRedirect(this.loginRequest);
    }

    public async loginSilent(): Promise<AuthResponse> {
        if (!clientApplication) {
            throw new Error('Initialize MSAL client application first.');
        }

        const token = await clientApplication.acquireTokenSilent(this.loginRequest);

        await tokenCache.cacheToken(LOCAL_STORAGE_KEY, token);

        await this.getKeychainKeys();
        await localStorageKeychain.rehash();

        this.keepUserAuthenticated();

        return token;
    }

    public acquireTokenRedirect() {
        if (!clientApplication) {
            throw new Error('Initialize MSAL client application first.');
        }

        clientApplication.acquireTokenRedirect(this.loginRequest);
    }

    public getCachedToken = async (
        expiration: number = 60
    ): Promise<ITokenWithMsalAccount | undefined> => {
        await this.initializeSignal.promise;

        const cachedToken = await tokenCache.getCachedToken(LOCAL_STORAGE_KEY, expiration);
        if (cachedToken) {
            const expirationTime = getTokenExpiration(cachedToken);

            if (expirationTime >= expiration) {
                if (expirationTime <= expirationTimeBackgroundTokenRefreshThreshold) {
                    this.acquireTokenSilent();
                }

                return cachedToken as ITokenWithMsalAccount;
            }
        }

        try {
            return await this.acquireToken();
        } catch (e) {
            autServiceTrace.error(e);
            sendTelemetry('vsonline/auth/acquire-token/error', e);
        }

        return undefined;
    };

    private tokenAcquirePromise: Promise<ITokenWithMsalAccount | undefined> | undefined;

    private async acquireToken(): Promise<ITokenWithMsalAccount | undefined> {
        if (!this.tokenAcquirePromise) {
            this.tokenAcquirePromise = this.acquireTokenInternal();
        }

        const token = await this.tokenAcquirePromise;
        this.tokenAcquirePromise = undefined;
        return token;
    }

    private async acquireTokenSilent(): Promise<ITokenWithMsalAccount | undefined> {
        try {
            const token = await acquireTokenSilent(SCOPES);

            if (!token) {
                return;
            }

            await tokenCache.cacheToken(LOCAL_STORAGE_KEY, token);

            const user = getUserFromMsalToken(token);
            getAuthTokenSuccessAction(token.accessToken, user);

            return token;
        } catch (e) {
            return;
        }
    }

    private async acquireTokenInternal(): Promise<ITokenWithMsalAccount | undefined> {
        try {
            const token = await acquireTokenSilentWith2FA(SCOPES);
            if (!token) {
                return;
            }

            const user = getUserFromMsalToken(token);
            await tokenCache.cacheToken(LOCAL_STORAGE_KEY, token);
            getAuthTokenSuccessAction(token.accessToken, user);

            return token;
        } catch (e) {
            autServiceTrace.verbose(e);
        }
    }

    public async logout() {
        await tokenCache.clearCache();
        logoutFromArmAuthService();

        if (clientApplication) {
            clientApplication.logout();
        }

        if (this.keepUserAuthenticated) {
            this.keepUserAuthenticated.stop();
        }
    }

    public async getARMToken(expiration: number, timeout: number = 10000): Promise<IToken | null> {
        await this.initializeSignal.promise;
        const cachedToken = await this.getCachedToken(expiration);

        if (!cachedToken) {
            throw new Error('User is not authenticated.');
        }

        return await getARMToken(cachedToken, expiration, timeout);
    }

    private makeTokenRequest = async () => {
        const token = await this.getCachedToken();

        if (!token) {
            // try to refresh the token by doing redirect (in case token expired)
            return this.acquireTokenRedirect();
        }

        const keys = await createKeys(token.accessToken);

        if (!keys) {
            this.logout();
        }
    };

    /**
     * Function to poll the `getCachedToken` which has the side-effect of refreshing the auth token if needed.
     * This function is a debounced version of simple interval, hence it will call the `getCachedToken` function
     * after the `timeout` milliseconds of last `getCachedToken` token request.
     */
    private keepUserAuthenticated = debounceInterval(
        this.makeTokenRequest,
        5 * 60 * 1000 // 5 minutes
    );

    /**
     * A temporary spolution for Azure Acccount for ignite.
     * We should move to `refreshToken`-based solution in the nearest future.
     */
    public getAuthCode = async (): Promise<IAuthCode | null> => {
        await this.initializeSignal.promise;

        const cachedToken = (await authService.getCachedToken(60)) as ITokenWithMsalAccount;

        if (!cachedToken) {
            throw new Error('User is not authenticated.');
        }

        const authCode = await getFreshArmAuthCodeForTenant(cachedToken, 'common');

        sendTelemetry('vsonline/auth/acquire-auth-code', { isCodeAcquired: !!authCode });

        if (!authCode) {
            return null;
        }

        return {
            code: authCode,
            redirectionUrl: location.origin,
        };
    };
}

export const authService = new AuthService();
