
import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';
import { inLocalStorageJWTTokenCacheFactory } from '../cache/localstorageJWTCache';
import { getTokenExpiration } from '../utils/getTokenExpiration';
import { expirationTimeBackgroundTokenRefreshThreshold } from '../constants';
import { debounceInterval } from '../utils/debounce-interval';

import { logout as logoutFromArmAuthService, getARMToken, getFreshArmAuthCodeForTenant } from './authARMService';
import { getAuthTokenSuccessAction } from '../actions/getAuthTokenActions';
import { IToken } from '../typings/IToken';

import { setIsInternal } from './isInternalUserTracker';
import { sendTelemetry } from '../utils/telemetry';
import { clientApplication } from './msalConfig';
import { acquireToken, acquireTokenSilent } from './acquireToken';

import { autServiceTrace } from './autServiceTrace';

const SCOPES = ['email openid offline_access api://9db1d849-f699-4cfb-8160-64bed3335c72/All'];

const LOCAL_STORAGE_KEY = 'vsonline.default.account';

const tokenCache = inLocalStorageJWTTokenCacheFactory();

interface IAuthCode {
    code: string;
    redirectionUrl: string;
}

tokenCache.onTokenChange(({ name, token }) => {
    if (!token) {
        setIsInternal(false);
        return;
    }

    const expirationTime = getTokenExpiration(token);
    if (expirationTime <= 0) {
        setIsInternal(false);
        return;
    }

    const { email, preferred_username } = (token as ITokenWithMsalAccount).account.idTokenClaims;
    const userEmail = email || preferred_username;

    setIsInternal(!!(userEmail && userEmail.includes('@microsoft.com')));
});

class AuthService {
    public async login() {
        const loginRequest = {
            scopes: SCOPES,
        };

        await clientApplication.loginPopup(loginRequest);
        const token = await this.acquireToken();

        return token;
    }

    public getCachedToken = async (expiration: number = 60): Promise<ITokenWithMsalAccount | undefined> => {
        this.keepUserAuthenticated();

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
    }

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
            getAuthTokenSuccessAction(token);

            return token;
        } catch (e) {
            return;
        }
    }


    private async acquireTokenInternal(): Promise<ITokenWithMsalAccount | undefined> {
        try {
            const token = await acquireToken(SCOPES);
            await tokenCache.cacheToken(LOCAL_STORAGE_KEY, token);
            getAuthTokenSuccessAction(token);

            return token;
        } catch (e) {
            return undefined;
        }
    }

    public async logout() {
        await tokenCache.clearCache();
        logoutFromArmAuthService();

        if (this.keepUserAuthenticated) {
            this.keepUserAuthenticated.stop();
        }
    }

    public async getARMToken(expiration: number, timeout: number = 10000): Promise<IToken | null> {
        const cachedToken = await this.getCachedToken(expiration);

        if (!cachedToken) {
            throw new Error('User is not authenticated.')
        }

        return await getARMToken(cachedToken, expiration, timeout);
    }

    /**
     * Function to poll the `getCachedToken` which has the side-effect of refreshing the auth token if needed.
     * This function is a debounced version of simple interval, hence it will call the `getCachedToken` function
     * after the `timeout` milliseconds of last `getCachedToken` token request.
     */
    private keepUserAuthenticated = debounceInterval(this.getCachedToken, 5 * 60 * 1000 /* 5 minutes */);

    /**
     * A temporary spolution for Azure Acccount for ignite.
     * We should move to `refreshToken`-based solution in the nearest future.
     */
    getAuthCode = async (): Promise<IAuthCode | null> => {
        const cachedToken = await authService.getCachedToken(60) as ITokenWithMsalAccount;

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
            redirectionUrl: location.origin
        }
    }
}

export const authService = new AuthService();
