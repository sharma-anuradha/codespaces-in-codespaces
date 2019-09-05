import * as msal from 'msal';
import { UserAgentApplication, AuthResponse } from 'msal';
import jwtDecode from 'jwt-decode';
import { trace as baseTrace } from '../utils/trace';

const error = baseTrace.extend('authService:error');

// tslint:disable-next-line: no-console
error.log = console.log.bind(console);

const SCOPES = ['openid offline_access api://9db1d849-f699-4cfb-8160-64bed3335c72/All'];

const msalConfig: msal.Configuration = {
    auth: {
        clientId: 'a3037261-2c94-4a2e-b53f-090f6cdd712a',
        authority: 'https://login.microsoftonline.com/common',
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

export interface IToken {
    accessToken: string;
    expiresOn: Date;
    account: msal.Account;
}

interface ITokensMemoryCache {
    [key: string]: IToken;
}

export const getTokenExpiration = (token: IToken): number => {
    const { expiresOn } = token;
    const seconds = (new Date(expiresOn).getTime() - Date.now()) / 1000;

    return Math.floor(seconds);
};

const clientApplication = new UserAgentApplication(msalConfig);

class AuthService {
    private tokens: ITokensMemoryCache = {};

    public async signIn() {
        const loginRequest = {
            scopes: SCOPES,
        };

        await clientApplication.loginPopup(loginRequest);
        const token = await this.acquireToken();

        return token;
    }

    public async getCachedToken(expiration: number = 1): Promise<IToken | undefined> {
        // get token in memory
        for (let [_, token] of Object.entries(this.tokens)) {
            const expiresIn = getTokenExpiration(token);

            if (expiresIn > expiration) {
                if (expiresIn <= 1800) {
                    this.acquireToken();
                }
                return token;
            }
        }

        // get token in local storage
        const lsRecord = localStorage.getItem(LOCAL_STORAGE_KEY);
        if (lsRecord) {
            try {
                const token = JSON.parse(lsRecord) as IToken;
                const expiresIn = getTokenExpiration(token);

                if (expiresIn > expiration) {
                    if (expiresIn <= 3000) {
                        this.acquireToken();
                    }
                    return token;
                }
            } catch (e) {
                error('Parsing the LS record', e);
            }
        }

        try {
            return await this.acquireToken();
        } catch (e) {
            error(e);
        }

        return undefined;
    }

    private tokenAcquirePromise: Promise<IToken | undefined> | undefined;

    private async acquireToken(): Promise<IToken | undefined> {
        if (!this.tokenAcquirePromise) {
            this.tokenAcquirePromise = this.acquireTokenInternal();
        }

        const result = await this.tokenAcquirePromise;
        this.tokenAcquirePromise = undefined;
        return result;
    }

    private async acquireTokenInternal(): Promise<IToken | undefined> {
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

    private cacheToken(token: IToken) {
        const accountId = token.account.accountIdentifier;

        this.tokens[accountId] = token;

        localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(token));
    }

    public async signOut() {
        const debugSetting = localStorage.debug;
        localStorage.clear();
        localStorage.debug = debugSetting;
        this.tokens = {};
    }
}

export const authService = new AuthService();

export async function acquireToken(scopes: string[]) {
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

function tokenFromTokenResponse(tokenResponse: AuthResponse): IToken {
    const { accessToken, account } = tokenResponse;

    const jwtToken = jwtDecode(accessToken) as { exp: number };
    const msTime = (jwtToken.exp - 10) * 1000;

    const token = {
        accessToken,
        expiresOn: new Date(msTime),

        account,
    };

    return token;
}
