import { createUniqueId } from '../dependencies';
import { createTrace } from '../utils/createTrace';
import { Signal } from '../utils/signal';
import { Disposable } from 'vscode-jsonrpc';

export const trace = createTrace('GitHubCredentialService');

const localStorageKey = 'githubAccessToken';

type GitHubAccessTokenResponse = {
    readonly accessToken: string;
    readonly state: string;
};

export function storeGitHubAccessTokenResponse({ accessToken, state }: GitHubAccessTokenResponse) {
    localStorage.setItem(localStorageKey, JSON.stringify({ accessToken, state }));
}

function clearGitHubAccessTokenResponse() {
    localStorage.removeItem(localStorageKey);
}

function getStoredGitHubAccessTokenResponse(): GitHubAccessTokenResponse | null {
    const storedTokenString = localStorage.getItem(localStorageKey);

    if (!storedTokenString) {
        return null;
    }

    try {
        const parsedToken = JSON.parse(storedTokenString);
        if (typeof parsedToken.accessToken !== 'string') {
            localStorage.removeItem(localStorageKey);
            return null;
        }
        if (typeof parsedToken.state !== 'string') {
            localStorage.removeItem(localStorageKey);
            return null;
        }

        return parsedToken;
    } catch {
        return null;
    }
}

export function getStoredGitHubToken(): string | null {
    const storedToken = getStoredGitHubAccessTokenResponse();

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

    authenticate(): Promise<string | null> {
        if (this.tokenRequest) {
            return this.tokenRequest.promise;
        }

        const storedToken = getStoredGitHubToken();
        if (storedToken) {
            return Promise.resolve(storedToken);
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

        const resolveWithToken = (event: StorageEvent) => {
            if (event.key === localStorageKey) {
                window.removeEventListener('storage', resolveWithToken);

                clearTimeout(timeout);

                this.tokenRequest = undefined;
                const response = getStoredGitHubAccessTokenResponse();
                if (!response) {
                    currentTokenRequest.complete(null);
                    return;
                }
                if (response.state !== this.state) {
                    currentTokenRequest.complete(null);
                    clearGitHubAccessTokenResponse();

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
