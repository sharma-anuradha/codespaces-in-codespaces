import * as msal from 'msal';
import { UserAgentApplication, AuthResponse } from 'msal';
import jwtDecode from 'jwt-decode';
import { trace as baseTrace } from '../utils/trace';
import { ITokenWithMsalAccount } from '../typings/ITokenWithMsalAccount';
import { inLocalStorageJWTTokenCacheFactory } from '../cache/localstorageJWTCache';
import { getTokenExpiration } from '../utils/getTokenExpiration';
import { expirationTimeBackgroundTokenRefreshThreshold, aadAuthorityUrlCommon } from '../constants';

import { logout as logoutFromArmAuthService, getARMToken } from './authARMService';

const error = baseTrace.extend('authService:error');

// tslint:disable-next-line: no-console
error.log = console.log.bind(console);

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

    public async getCachedToken(expiration: number = expirationTimeBackgroundTokenRefreshThreshold): Promise<ITokenWithMsalAccount | undefined> {
        const cachedToken = tokenCache.getCachedToken(LOCAL_STORAGE_KEY, expiration);

        if (cachedToken) {
            const expirationTime = getTokenExpiration(cachedToken);
            if (expirationTime <= expirationTimeBackgroundTokenRefreshThreshold) {
                this.acquireToken();
            }
            return cachedToken as ITokenWithMsalAccount;
        }
        
        try {
            return await this.acquireToken();
        } catch (e) {
            error(e);
        }

        return undefined;
    }

    private tokenAcquirePromise: Promise<ITokenWithMsalAccount | undefined> | undefined;

    private async acquireToken(): Promise<ITokenWithMsalAccount | undefined> {
        if (!this.tokenAcquirePromise) {
            this.tokenAcquirePromise = this.acquireTokenInternal();
        }

        const result = await this.tokenAcquirePromise;
        this.tokenAcquirePromise = undefined;
        return result;
    }

    private async acquireTokenInternal(): Promise<ITokenWithMsalAccount | undefined> {
        const tokenRequest = {
            scopes: SCOPES,
            authority: msalConfig.auth.authority,
        };

        try {
            const token = await acquireToken(tokenRequest.scopes);
            this.cacheToken(token);

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
    }
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
        if (err.name === 'InteractionRequiredAuthError') {
            tokenResponse = await clientApplication.acquireTokenPopup(tokenRequest);
        } else {
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
