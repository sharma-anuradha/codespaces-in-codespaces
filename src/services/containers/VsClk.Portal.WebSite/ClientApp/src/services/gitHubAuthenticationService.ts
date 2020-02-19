import { Disposable } from 'vscode-jsonrpc';

import { createUniqueId } from '../dependencies';
import { createTrace } from '../utils/createTrace';
import { Signal } from '../utils/signal';
import { localStorageKeychain } from '../cache/localStorageKeychainInstance';

export const trace = createTrace('GitHubCredentialService');

const unencryptedLocalStorageKey = 'githubAccessToken';
const gitHubLocalStorageKey = 'githubAccessTokenEncrypted';

type GitHubAccessTokenResponse = {
    readonly accessToken: string;
    readonly state: string;
    readonly scope?: string;
};

export function isGitHubTokenUpdate(event: StorageEvent) {
    return event.key === gitHubLocalStorageKey;
}

export async function storeGitHubAccessTokenResponse({
    accessToken,
    state,
    scope,
}: GitHubAccessTokenResponse) {
    await localStorageKeychain.set(
        gitHubLocalStorageKey,
        JSON.stringify({ accessToken, state, scope })
    );
}

async function clearGitHubAccessTokenResponse() {
    await localStorageKeychain.delete(gitHubLocalStorageKey);
}

async function getStoredGitHubAccessTokenResponse(): Promise<GitHubAccessTokenResponse | null> {
    // We migrate the existing token first for background compatibility
    const unencryptedToken = localStorage.getItem(unencryptedLocalStorageKey);
    if (unencryptedToken) {
        localStorage.removeItem(unencryptedLocalStorageKey);
        await localStorageKeychain.set(gitHubLocalStorageKey, unencryptedToken);
    }

    const storedTokenString =
        unencryptedToken || (await localStorageKeychain.get(gitHubLocalStorageKey));
    if (!storedTokenString) {
        return null;
    }

    try {
        const parsedToken = JSON.parse(storedTokenString);
        if (typeof parsedToken.accessToken !== 'string') {
            await localStorageKeychain.delete(gitHubLocalStorageKey);
            return null;
        }
        if (typeof parsedToken.state !== 'string') {
            await localStorageKeychain.delete(gitHubLocalStorageKey);
            return null;
        }

        return parsedToken;
    } catch {
        return null;
    }
}

export async function getStoredGitHubToken(scope: string | null = null): Promise<string | null> {
    const storedToken = await getStoredGitHubAccessTokenResponse();

    if (scope) {
        return storedToken && storedToken.scope && storedToken.scope.includes(scope)
            ? storedToken.accessToken
            : null;
    }

    return storedToken && storedToken.accessToken;
}

let currentAttempt: GithubAuthenticationAttempt | undefined;
export async function getGitHubAccessToken(): Promise<string | null> {
    if (currentAttempt) {
        return await currentAttempt.authenticate();
    }

    currentAttempt = new GithubAuthenticationAttempt();
    const authPromise = currentAttempt.authenticate();

    authPromise.finally(() => {
        currentAttempt = undefined;
    });

    return await authPromise;
}

export class GithubAuthenticationAttempt implements Disposable {
    private tokenRequest?: Signal<string | null>;

    get url() {
        return `${window.location.origin}/github-auth?state=${encodeURIComponent(this.state)}`;
    }

    get target() {
        return '_github_auth_window';
    }

    constructor(private readonly state = createUniqueId()) {}

    async authenticate(): Promise<string | null> {
        if (this.tokenRequest) {
            return this.tokenRequest.promise;
        }

        const storedToken = await getStoredGitHubToken();
        if (storedToken) {
            return storedToken;
        }

        this.tokenRequest = new Signal();
        const currentTokenRequest = this.tokenRequest;

        window.open(this.url, this.target);

        const timeout = setTimeout(() => {
            this.tokenRequest = undefined;
            currentTokenRequest.reject(
                new Error('Failed to acquire GitHub credentials. Reason: timeout.')
            );
        }, 5 * 60 * 1000);

        currentTokenRequest.promise.finally(() => {
            clearTimeout(timeout);
        });

        const resolveWithToken = async (event: StorageEvent) => {
            if (isGitHubTokenUpdate(event)) {
                window.removeEventListener('storage', resolveWithToken);

                clearTimeout(timeout);

                this.tokenRequest = undefined;
                const response = await getStoredGitHubAccessTokenResponse();
                if (!response) {
                    currentTokenRequest.complete(null);
                    return;
                }
                if (response.state !== this.state) {
                    currentTokenRequest.complete(null);
                    await clearGitHubAccessTokenResponse();

                    currentTokenRequest.reject(
                        new Error('Nonce from the GitHub request does not match.')
                    );
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
