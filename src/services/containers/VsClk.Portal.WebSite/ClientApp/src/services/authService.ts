import * as msal from 'msal';
import { UserAgentApplication, AuthResponse } from 'msal';
import jwtDecode from 'jwt-decode';

import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';
import { inLocalStorageJWTTokenCacheFactory } from '../cache/localstorageJWTCache';
import { getTokenExpiration } from '../utils/getTokenExpiration';
import { expirationTimeBackgroundTokenRefreshThreshold, aadAuthorityUrlCommon } from '../constants';
import { debounceInterval } from '../utils/debounce-interval';

import { logout as logoutFromArmAuthService, getARMToken } from './authARMService';
import { getAuthTokenSuccessAction } from '../actions/getAuthToken';
import { createTrace } from '../utils/createTrace';

const trace = createTrace('AuthService');

const SCOPES = ['email openid offline_access api://9db1d849-f699-4cfb-8160-64bed3335c72/All'];

const msalConfig: msal.Configuration = {
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

const LOCAL_STORAGE_KEY = 'vsonline.default.account';

export const clientApplication = new UserAgentApplication(msalConfig);

const tokenCache = inLocalStorageJWTTokenCacheFactory();

class AuthService {
    public async login() {
        const loginRequest = {
            scopes: SCOPES,
        };

        await clientApplication.loginPopup(loginRequest);
        const token = await this.acquireToken();

        // try to get arm token instantly since
        // this can trigger the second popup window
        await getARMToken(60 * 10);

        return token;
    }

    public getCachedToken = async (expiration: number = 60): Promise<ITokenWithMsalAccount | undefined> => {
        const cachedToken = tokenCache.getCachedToken(LOCAL_STORAGE_KEY, expiration);

        this.keepUserAuthenticated();
        
        if (cachedToken) {
            const expirationTime = getTokenExpiration(cachedToken);
            
            if (expirationTime > expiration) {
                if (expirationTime <= expirationTimeBackgroundTokenRefreshThreshold) {
                    this.acquireToken();
                }

                return cachedToken as ITokenWithMsalAccount;
            }
        }
        
        try {
            return await this.acquireToken();
        } catch (e) {
            trace.error(e);
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

    private async acquireTokenInternal(): Promise<ITokenWithMsalAccount | undefined> {
        try {
            const token = await acquireToken(SCOPES);
            this.cacheToken(token);
            getAuthTokenSuccessAction(token);

            return token;
        } catch (e) {
            return undefined;
        }
    }

    private cacheToken(token: ITokenWithMsalAccount) {
        tokenCache.cacheToken(LOCAL_STORAGE_KEY, token);
    }

    public async logout() {
        tokenCache.clearCache();
        logoutFromArmAuthService();

        if (this.keepUserAuthenticated) {
            this.keepUserAuthenticated.stop();
        }
    }

    /**
     * Function to poll the `getCachedToken` which has the side-effect of refreshing the auth token if needed.
     * This function is a debounced version of simple interval, hence it will call the `getCachedToken` function
     * after the `timeout` milliseconds of last `getCachedToken` token request.
     */
    private keepUserAuthenticated = debounceInterval(this.getCachedToken, 5 * 60 * 1000 /* 5 minutes */);
}

export const authService = new AuthService();

export async function acquireToken(scopes: string[]): Promise<ITokenWithMsalAccount> {
    const tokenRequest = {
        scopes,
        authority: msalConfig.auth.authority,
    };

    let tokenResponse: AuthResponse;
    try {
        tokenResponse = await clientApplication.acquireTokenSilent(tokenRequest);
    } catch (err) {
        trace.warn(err);
        if (err.name === 'InteractionRequiredAuthError') {
            tokenResponse = await clientApplication.acquireTokenPopup(tokenRequest);
        } else {
            trace.error(err);
            throw err;
        }
    }

    return tokenFromTokenResponse(tokenResponse);
}

export function tokenFromTokenResponse(tokenResponse: AuthResponse): ITokenWithMsalAccount {
    const { accessToken, account } = tokenResponse;

    let msTime = 0;
    try {
        const jwtToken = jwtDecode(accessToken) as { exp: number };
        msTime = (jwtToken.exp - 10) * 1000;
    } catch {/* ignore */}

    const token = {
        accessToken,
        expiresOn: new Date(msTime),
        account,
    };

    return token;
}
