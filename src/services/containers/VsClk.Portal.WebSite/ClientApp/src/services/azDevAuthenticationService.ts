import { createUniqueId } from '../dependencies';
import { createTrace } from '../utils/createTrace';
import { Signal } from '../utils/signal';
import { IAuthenticationAttempt } from './authenticationServiceBase';
import { SupportedGitService } from '../utils/gitUrlNormalization';
import { localStorageKeychain } from '../cache/localStorageKeychainInstance';

export const trace = createTrace('AzDevCredentialService');

export const azDevLocalStorageKey = 'azDevAccessTokenEncrypted';

type AzDevAccessTokenResponse = {
    readonly accessToken: string;
    readonly state: string;
    readonly scope?: string;
    readonly refreshToken: string;
    readonly expiresOn: Date;
};

export async function storeAzDevAccessTokenResponse({
    accessToken,
    state,
    scope,
    refreshToken,
    expiresOn,
}: AzDevAccessTokenResponse) {
    debugger;
    await localStorageKeychain.set(azDevLocalStorageKey, JSON.stringify({ accessToken, state, scope, refreshToken, expiresOn: expiresOn.getTime() }));
}

async function clearAzDevAccessTokenResponse() {
    await localStorageKeychain.delete(azDevLocalStorageKey);
}

async function getStoredAzDevAccessTokenResponse(): Promise<AzDevAccessTokenResponse | null> {
    const storedTokenString = await localStorageKeychain.get(azDevLocalStorageKey);

    if (!storedTokenString) {
        return null;
    }

    try {
        const parsedToken = JSON.parse(storedTokenString);
        if (typeof parsedToken.accessToken !== 'string') {
            await localStorageKeychain.delete(azDevLocalStorageKey);
            return null;
        }
        if (typeof parsedToken.state !== 'string') {
            await localStorageKeychain.delete(azDevLocalStorageKey);
            return null;
        }
        if (typeof parsedToken.refreshToken !== 'string') {
            await localStorageKeychain.delete(azDevLocalStorageKey);
            return null;
        }
        if (typeof parsedToken.expiresOn !== 'number') {
            await localStorageKeychain.delete(azDevLocalStorageKey);
            return null;
        } else {
            parsedToken.expiresOn = new Date(parsedToken.expiresOn);
        }

        return parsedToken;
    } catch {
        return null;
    }
}

export async function getStoredAzDevToken(scope: string | null = null): Promise<string | null> {
    const storedToken = await getStoredAzDevAccessTokenResponse();

    // TODO #1069280: Use refresh token to generate AccessToken without opening a new tab
    if (scope) {
        return storedToken && storedToken.scope && storedToken.scope.includes(scope) && storedToken.expiresOn && storedToken.expiresOn > new Date()
            ? storedToken.accessToken
            : null;
    }

    return storedToken && storedToken.accessToken;
}

let currentAttempt: AzDevAuthenticationAttempt | undefined;
export async function getAzDevAccessToken(): Promise<string | null> {
    if (currentAttempt) {
        return await currentAttempt.authenticate();
    }

    currentAttempt = new AzDevAuthenticationAttempt();
    const authPromise = currentAttempt.authenticate();

    authPromise.finally(() => {
        currentAttempt = undefined;
    });

    return await authPromise;
}

export class AzDevAuthenticationAttempt implements IAuthenticationAttempt {
    private tokenRequest?: Signal<string | null>;

    get url() {
        return `${window.location.origin}/azdev-auth?state=${encodeURIComponent(this.state)}`;
    }

    get target() {
        return '_azdev_auth_window';
    }
    
    get gitServiceType(): SupportedGitService {
        return SupportedGitService.AzureDevOps;
    }

    constructor(private readonly state = createUniqueId()) {}

    async authenticate(): Promise<string | null> {
        if (this.tokenRequest) {
            return this.tokenRequest.promise;
        }

        const storedTokenResponse = await getStoredAzDevAccessTokenResponse();
        if (storedTokenResponse && storedTokenResponse.accessToken) {
            if (storedTokenResponse.expiresOn > new Date()) {
                return Promise.resolve(storedTokenResponse.accessToken);
            } else {
                // TODO: Temporaryly clear token so that auth window will open again.
                //       In future do a request to get a new accessToken using refresh token.
                await clearAzDevAccessTokenResponse();
            }
        }

        this.tokenRequest = new Signal();
        const currentTokenRequest = this.tokenRequest;

        window.open(this.url, this.target);

        const timeout = setTimeout(() => {
            window.removeEventListener('storage', resolveWithToken);
            this.tokenRequest = undefined;
            currentTokenRequest.reject(
                new Error('Failed to acquire Azure DevOps credentials. Reason: timeout.')
            );
        }, 5 * 60 * 1000);

        currentTokenRequest.promise.finally(() => {
            clearTimeout(timeout);
        });

        const resolveWithToken = async(event: StorageEvent) => {
            if (event.key === azDevLocalStorageKey) {
                window.removeEventListener('storage', resolveWithToken);

                clearTimeout(timeout);

                this.tokenRequest = undefined;
                const response = await getStoredAzDevAccessTokenResponse();
                if (!response) {
                    currentTokenRequest.complete(null);
                    return;
                }

                currentTokenRequest.complete(response.accessToken);
                return;
            }
        };

        window.addEventListener('storage', resolveWithToken);

        return currentTokenRequest.promise;
    }

    dispose(): void {
        if (this.tokenRequest && !this.tokenRequest.isFulfilled) {
            this.tokenRequest.cancel();
        }
    }
}
