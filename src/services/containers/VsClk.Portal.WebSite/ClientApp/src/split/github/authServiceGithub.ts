import { addDefaultGithubKey, Signal, localStorageKeychain, setKeychainKeys, createKeys, getCurrentEnvironmentId } from 'vso-client-core';

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
import { HOUR_MS } from 'vso-client-core/src/constants';

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

        this.repoInfo = PostMessageRepoInfoRetriever.getStoredInfo();
        if (this.repoInfo) {
            this.initializeSignal.complete(undefined);
            return;
        }

        window.onload = async () => {
            const postMessageInfoRetriever = new PostMessageRepoInfoRetriever();

            this.repoInfo = await postMessageInfoRetriever.getRepoInfo();

            postMessageInfoRetriever.dispose();
            
            this.initializeSignal.complete(undefined);
        };
    };

    private getGitHubToken = async (repositoryId: string) => {
        const token = await this.getCachedGitHubToken();
        if (token) {
            return token;
        }

        const githubTokenResponse = await getStoredGitHubAccessTokenResponse();

        if (!githubTokenResponse || (githubTokenResponse.repoId !== repositoryId)) {
            clearGitHubAccessTokenResponse();

            return await getGitHubAccessToken(true, location.pathname);
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
            return cascadeToken;
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
    }

    private getFreshCascadeToken = async (): Promise<string | null> => {
        if (!this.repoInfo) {
            throw new Error('No repo info found.');
        }

        const githubToken = await this.getGitHubToken(this.repoInfo.repositoryId);
        if (!githubToken) {
            return null;
        }

        const { ownerUsername, workspaceId } = this.repoInfo;

        const url = new URL(`/workspaces/${ownerUsername}/${workspaceId}/token`, getGitHubApiEndpoint());
        const cascadeToken = await fetch(
            url.toString(),
            {
                method: 'POST',
                headers: {
                    Authorization: `Bearer ${githubToken}`,
                },
            }
        );

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

    public getCachedCascadeToken = async (expirationMs = 60 * 60 * 1000) => {
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

        const parsedToken = parseCascadeToken(token);
        if (!parsedToken) {
            await localStorageKeychain.delete(keychainKey);
            return null;
        }

        const tokenExpirationDelta = parsedToken.exp - Date.now();

        if (tokenExpirationDelta <= 5 * HOUR_MS) {
            return await this.getFreshCascadeToken();
        }

        return token;
    };
}

export const authService = new AuthServiceGithub();
