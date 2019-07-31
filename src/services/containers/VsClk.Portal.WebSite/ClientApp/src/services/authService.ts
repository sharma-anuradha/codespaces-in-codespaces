import * as msal from 'msal';
import jwtDecode from 'jwt-decode';

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

class AuthService {
    private clientApplication = new msal.UserAgentApplication(msalConfig);

    private tokens: ITokensMemoryCache = {};

    public async defaultSilentSignIn() {
        const token = await this.getCachedToken();

        return token;
    }

    public async signIn() {
        const loginRequest = {
            scopes: SCOPES,
        };

        await this.clientApplication.loginPopup(loginRequest);
        const token = await this.acquireToken();

        return token;
    }

    public async getCachedToken(expiration: number = 1): Promise<IToken | null> {
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
                console.log('Parsing the LS record', e);
            }
        }

        try {
            return await this.acquireToken();
        } catch (e) {
            console.log(e);
        }

        return null;
    }

    private tokenAcquirePromise: Promise<IToken | null> | null = null;

    public async acquireToken(): Promise<IToken | null> {
        if (!this.tokenAcquirePromise) {
            this.tokenAcquirePromise = this.acquireTokenInternal();
        }

        const result = await this.tokenAcquirePromise;
        this.tokenAcquirePromise = null;
        return result;
    }

    private async acquireTokenInternal(): Promise<IToken | null> {
        const tokenRequest = {
            scopes: SCOPES,
            authority: msalConfig.auth.authority,
        };

        try {
            const tokenResponse = await this.clientApplication.acquireTokenSilent(tokenRequest);

            const token = this.tokenFromTokenResponse(tokenResponse);
            this.cacheToken(token);

            return token;
        } catch (e) {
            return null;
        }
    }

    private tokenFromTokenResponse(tokenResponse: msal.AuthResponse): IToken {
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

    private cacheToken(token: IToken) {
        const accountId = token.account.accountIdentifier;

        this.tokens[accountId] = token;

        localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(token));
    }

    public async signOut() {
        localStorage.clear();
        this.tokens = {};
        // localStorage.removeItem(LOCAL_STORAGE_KEY);
        // this.clientApplication.logout();
    }
}

export const authService = new AuthService();
