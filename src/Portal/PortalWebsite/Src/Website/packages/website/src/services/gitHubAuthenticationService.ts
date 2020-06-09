import { createUniqueId } from '../dependencies';
import {
    Signal,
    createTrace,
    localStorageKeychain,
    isHostedOnGithub
} from 'vso-client-core';
import { SupportedGitService } from 'vso-ts-agent';

import { IAuthenticationAttempt } from './authenticationServiceBase';
import { IRepoInfo, PostMessageRepoInfoRetriever } from '../split/github/postMessageRepoInfoRetriever';

export const trace = createTrace('GitHubCredentialService');

const unencryptedLocalStorageKey = 'githubAccessToken';
const gitHubLocalStorageKey = 'githubAccessTokenEncrypted';

type GitHubAccessTokenResponse = {
    readonly accessToken: string;
    readonly state: string;
    readonly scope?: string;
    readonly repoId?: string;
};

export function isGitHubTokenUpdate(event: StorageEvent) {
    return event.key === gitHubLocalStorageKey;
}

export async function storeGitHubAccessTokenResponse({
    accessToken,
    state,
    scope,
    repoId,
}: GitHubAccessTokenResponse) {
    await localStorageKeychain.set(
        gitHubLocalStorageKey,
        JSON.stringify({ accessToken, state, scope, repoId })
    );
}

export async function clearGitHubAccessTokenResponse() {
    await localStorageKeychain.delete(gitHubLocalStorageKey);
}

export async function getStoredGitHubAccessTokenResponse(): Promise<GitHubAccessTokenResponse | null> {
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
export async function getGitHubAccessToken(
    isInline = false,
    redirectPath?: string,
): Promise<string | null> {
    if (currentAttempt) {
        return await currentAttempt.authenticate(isInline);
    }
    
    const state = (redirectPath)
        ? `${createUniqueId()},${redirectPath}`
        : undefined;

    currentAttempt = new GithubAuthenticationAttempt(state);
    const authPromise = currentAttempt.authenticate(isInline);

    authPromise.finally(() => {
        currentAttempt = undefined;
    });

    return await authPromise;
}

export class GithubAuthenticationAttempt implements IAuthenticationAttempt {
    private tokenRequest?: Signal<string | null>;

    get url() {
        /**
         * For GitHub hosted portal, we have to use the GitHub issued apps.
         * Since those apps are not OAuth apps that we already use, we have
         * to split the auth flow on using two differnet apps. The change might
         * be very disruptive and deserves a dedicated PR.
         */
        const client = (isHostedOnGithub())
            ? 'github'
            : 'vso';

        const params = new URLSearchParams([
            ['vso-client', client],
            ['state', this.state]
        ]);

        if (isHostedOnGithub()) {
            if (!this.repoInfo) {
                throw new Error('No repo info set, please get repo info first.')
            }
            params.append('repository_id', this.repoInfo.repositoryId);
        }

        return `${window.location.origin}/github-auth?${params}`;
    }

    get target() {
        return '_github_auth_window';
    }
    
    get gitServiceType(): SupportedGitService {
        return SupportedGitService.GitHub;
    }

    private repoInfo?: IRepoInfo;

    constructor(private readonly state = createUniqueId()) {}

    async authenticate(isInline = false): Promise<string | null> {
        if (this.tokenRequest) {
            return this.tokenRequest.promise;
        }

        const storedToken = await getStoredGitHubToken();
        if (storedToken) {
            return storedToken;
        }

        this.tokenRequest = new Signal();
        const currentTokenRequest = this.tokenRequest;

        if (isHostedOnGithub() && !this.repoInfo) {
            const postMessageRepoInfoRetriever = new PostMessageRepoInfoRetriever();
            this.repoInfo = await postMessageRepoInfoRetriever.getRepoInfo();

            postMessageRepoInfoRetriever.dispose();

            if (!this.repoInfo) {
                throw new Error('Cannot get repo info');
            }
        }

        if (isInline) {
            location.href = this.url;
        } else {
            window.open(this.url, this.target);
        }

        const timeout = setTimeout(() => {
            // TODO #1069288: Bug found after timeout the page shows does not show the error.
            window.removeEventListener('storage', resolveWithToken);
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
