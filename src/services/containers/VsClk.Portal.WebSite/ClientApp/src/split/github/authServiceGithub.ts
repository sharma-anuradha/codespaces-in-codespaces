import {
    getGitHubAccessToken,
    getStoredGitHubAccessTokenResponse,
    clearGitHubAccessTokenResponse,
} from '../../services/gitHubAuthenticationService';
import { addDefaultGithubKey } from '../../cache/localStorageKeychain/localstorageKeychainKeys';
import { Signal } from '../../utils/signal';
import { localStorageKeychain } from '../../cache/localStorageKeychainInstance';
import { PostMessageRepoInfoRetriever, IRepoInfo } from './postMessageRepoInfoRetriever';
import { parseCascadeToken } from './parseCascadeToken';

const vsoCascadeTokenKeychainKeyPrefix = 'vso-cascade-token';

export class AuthServiceGithub {
    private initializeSignal = new Signal();

    private repoInfo?: IRepoInfo;

    public init = async () => {
        /**
         * We temporary use static github encryption key
         * until backend supports Cascade token auth.
         */
        addDefaultGithubKey();  

        const postMessageInfoRetriever = new PostMessageRepoInfoRetriever();

        this.repoInfo = postMessageInfoRetriever.getStoredRepoInfo();
        if (this.repoInfo) {
            this.initializeSignal.complete(undefined);
            return;
        }

        window.onload = async () => {
            this.repoInfo = await postMessageInfoRetriever.getRepoInfo();
            this.initializeSignal.complete(undefined);
        };

        // ** Temporary disable until the service side support Cascade tokens
        // const keys = await fetchKeychainKeys();
        // if (keys) {
        //     setKeychainKeys(keys);
        // }
    };

    private createCascadeTokenKey(environmentId: string) {
        return `${vsoCascadeTokenKeychainKeyPrefix}_${environmentId}`;
    }

    public getCascadeToken = async () => {
        await this.initializeSignal.promise;

        if (!this.repoInfo) {
            throw new Error('No repo info found.');
        }

        const { repositoryId, environmentId } = this.repoInfo;

        const githubTokenResponse = await getStoredGitHubAccessTokenResponse();

        if (!githubTokenResponse || (githubTokenResponse.repoId !== repositoryId)) {
            clearGitHubAccessTokenResponse();

            return await getGitHubAccessToken(true, location.pathname);
        }

        const { accessToken } = githubTokenResponse;
        const cascadeToken = await this.getFreshCascadeToken(accessToken);

        if (!cascadeToken) {
            return cascadeToken;
        }

        // ** Temporary disable until the service side support Cascade tokens
        // const keys = await createKeys(cascadeToken);
        // setKeychainKeys(keys);

        const cascadeKeychainKey = this.createCascadeTokenKey(environmentId);
        await localStorageKeychain.set(cascadeKeychainKey, cascadeToken);

        return cascadeToken;
    };

    private getFreshCascadeToken = async (githubToken: string) => {
        if (!this.repoInfo) {
            throw new Error('No repo info found.');
        }

        const { ownerUsername, workspaceId } = this.repoInfo;

        const cascadeToken = await fetch(
            `https://api.github.com/workspaces/${ownerUsername}/${workspaceId}/token`,
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

        return token;
    };

    public getCachedCascadeToken = async (expirationMs = 60 * 60 * 1000) => {
        await this.initializeSignal.promise;

        if (!this.repoInfo) {
            throw new Error('No repo info found.');
        }

        const { environmentId } = this.repoInfo;

        const keychainKey = this.createCascadeTokenKey(environmentId);
        const token = await localStorageKeychain.get(keychainKey);

        if (!token) {
            return null;
        }

        const parsedToken = parseCascadeToken(token);
        if (!parsedToken) {
            await localStorageKeychain.delete(keychainKey);
            return null;
        }

        const tokenExpirationDelta = parsedToken.exp - Date.now();
        if (tokenExpirationDelta <= 0) {
            await localStorageKeychain.delete(keychainKey);
            return null;
        }

        if (tokenExpirationDelta < expirationMs) {
            return null;
        }

        return token;
    };
}

export const authService = new AuthServiceGithub();
