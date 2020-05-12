import {
    addDefaultGithubKey,
    Signal,
    localStorageKeychain,
    setKeychainKeys,
    createKeys,
    getCurrentEnvironmentId,
    timeConstants,
} from 'vso-client-core';

import {
    getGitHubAccessToken,
    getStoredGitHubAccessTokenResponse,
    clearGitHubAccessTokenResponse,
} from '../../services/gitHubAuthenticationService';

import { PostMessageRepoInfoRetriever, IRepoInfo } from './postMessageRepoInfoRetriever';
import { parseCascadeToken } from './parseCascadeToken';
import { getGitHubApiEndpoint } from '../../utils/getGitHubApiEndpoint';
import { fetchKeychainKeys } from '../../services/authService';
import { invalidateGitHubKey } from 'vso-client-core/src/keychain/localstorageKeychainKeys';
import { createCascadeTokenKey } from './createCascadeTokenKey';
import { createGitHubTokenKey } from './createGitHubTokenKey';
import { githubLoginPath } from './routesGithub';

export class AuthServiceGithub {
    private initializeSignal = new Signal();

    private repoInfo?: IRepoInfo;

    public init = async () => {
        const keys = await fetchKeychainKeys();
        if (keys) {
            setKeychainKeys(keys);
        } else {
            addDefaultGithubKey();
        }

        /**
         * If GitHub embedder does not pass the GitHub token, we perform OAuth redirections
         * to get the GitHub token with the OAuth app, in those circumstances, don't try to
         * get the repoInfo since there is no embedder anyway.
         * 
         * !Note! This should go after we fetching the encryption keys because we need
         *        those to set the encrypted GitHub token.
         */
        if (location.pathname === githubLoginPath) {
            return;
        }

        this.repoInfo = PostMessageRepoInfoRetriever.getStoredInfo();
        if (this.repoInfo) {
            this.initializeSignal.complete(undefined);
            return;
        }

        const postMessageInfoRetriever = new PostMessageRepoInfoRetriever();

        this.repoInfo = await postMessageInfoRetriever.getRepoInfo();
        postMessageInfoRetriever.dispose();

        // if tokens were passed, cache them
        const { githubToken, cascadeToken } = this.repoInfo;
        const environmentId = getCurrentEnvironmentId();

        if (typeof githubToken === 'string') {
            const keychainKey = createGitHubTokenKey(environmentId);
            await localStorageKeychain.set(keychainKey, githubToken);
        }

        if (typeof cascadeToken === 'string') {
            const keychainKey = createCascadeTokenKey(environmentId);
            const isValidToken = this.isValidCascadeToken(cascadeToken);

            if (isValidToken) {
                await localStorageKeychain.set(keychainKey, cascadeToken);
            }
        }

        this.initializeSignal.complete(undefined);
    };

    private getGitHubToken = async (repositoryId: string) => {
        const token = await this.getCachedGitHubToken();
        if (token) {
            return token;
        }

        const githubTokenResponse = await getStoredGitHubAccessTokenResponse();

        if (!githubTokenResponse || githubTokenResponse.repoId !== repositoryId) {
            clearGitHubAccessTokenResponse();

            const redirectionUrl = location.pathname + location.search;
            return await getGitHubAccessToken(true, redirectionUrl);
        }

        const { accessToken } = githubTokenResponse;

        return accessToken;
    };

    public getCascadeToken = async () => {
        await this.initializeSignal.promise;

        if (!this.repoInfo) {
            throw new Error('No repo info found.');
        }

        const { repositoryId, environmentId } = this.repoInfo;

        const accessToken = await this.getGitHubToken(repositoryId);
        if (!accessToken) {
            throw new Error('No access token found.');
        }

        const cascadeToken = await this.getFreshCascadeToken();
        if (!cascadeToken) {
            return null;
        }

        await this.fetchKeychainKeys(cascadeToken);

        const cascadeKeychainKey = createCascadeTokenKey(environmentId);
        await localStorageKeychain.set(cascadeKeychainKey, cascadeToken);

        const githubKeychainKey = createGitHubTokenKey(environmentId);
        await localStorageKeychain.set(githubKeychainKey, accessToken);

        return cascadeToken;
    };

    private fetchKeychainKeys = async (cascadeToken: string) => {
        const keys = await createKeys(cascadeToken);
        setKeychainKeys(keys);
        invalidateGitHubKey();

        await localStorageKeychain.rehash();
    };

    private getFreshCascadeToken = async (): Promise<string | null> => {
        if (!this.repoInfo) {
            throw new Error('No repo info found.');
        }

        const githubToken = await this.getGitHubToken(this.repoInfo.repositoryId);
        if (!githubToken) {
            return null;
        }

        const { ownerUsername, workspaceId } = this.repoInfo;

        const url = new URL(
            `/workspaces/${ownerUsername}/${workspaceId}/token`,
            getGitHubApiEndpoint()
        );
        const cascadeToken = await fetch(url.toString(), {
            method: 'POST',
            headers: {
                Authorization: `Bearer ${githubToken}`,
            },
        });

        if (!cascadeToken.ok) {
            return null;
        }
        const resultJson = await cascadeToken.json();
        const { token } = resultJson;

        if (!token) {
            return null;
        }

        const cascadeKeychainKey = createCascadeTokenKey(getCurrentEnvironmentId());
        await localStorageKeychain.set(cascadeKeychainKey, token);

        const githubKeychainKey = createGitHubTokenKey(getCurrentEnvironmentId());
        await localStorageKeychain.set(githubKeychainKey, githubToken);

        return token;
    };

    public getCachedGitHubToken = async () => {
        await this.initializeSignal.promise;

        if (!this.repoInfo) {
            throw new Error('No repo info found.');
        }

        const { environmentId } = this.repoInfo;

        const keychainKey = createGitHubTokenKey(environmentId);
        const token = await localStorageKeychain.get(keychainKey);

        if (!token) {
            return null;
        }

        return token;
    };

    public getCachedCascadeToken = async () => {
        await this.initializeSignal.promise;

        if (!this.repoInfo) {
            throw new Error('No repo info found.');
        }

        const { environmentId } = this.repoInfo;

        const keychainKey = createCascadeTokenKey(environmentId);
        const token = await localStorageKeychain.get(keychainKey);

        if (!token) {
            return await this.getFreshCascadeToken();
        }

        const isValidToken = this.isValidCascadeToken(token);
        if (!isValidToken) {
            return await this.getFreshCascadeToken();
        }

        return token;
    };

    private isValidCascadeToken = (cascadeToken: string) => {
        const parsedToken = parseCascadeToken(cascadeToken);
        if (!parsedToken) {
            return false;
        }

        const tokenExpirationDelta = parsedToken.exp - Date.now();
        return tokenExpirationDelta > 2.1 * timeConstants.HOUR_MS;
    };
}

export const authService = new AuthServiceGithub();
