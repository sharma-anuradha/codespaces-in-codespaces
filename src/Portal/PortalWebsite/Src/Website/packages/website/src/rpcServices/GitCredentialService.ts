import {
    createTrace,
    isHostedOnGithub,
    getCurrentEnvironmentId,
    localStorageKeychain
} from 'vso-client-core';

import {
    GitCredentialService as GitCredentialServiceBase,
    SupportedGitService,
} from 'vso-ts-agent';

import { createGitHubTokenKey } from '../split/github/createGitHubTokenKey';
import { getGitHubAccessToken } from '../services/gitHubAuthenticationService';
import { getAzDevAccessToken } from '../services/azDevAuthenticationService';

export const trace = createTrace('GitCredentialService');

export class GitCredentialService extends GitCredentialServiceBase {
    public async getTokenByHost(supportedGitService: SupportedGitService): Promise<string | null> {
        switch (supportedGitService) {
            case SupportedGitService.GitHub: {
                if (isHostedOnGithub()) {
                    const key = createGitHubTokenKey(getCurrentEnvironmentId());
                    const token = await localStorageKeychain.get(key);
                    
                    if (token) {
                        return token;
                    }
                }

                return await getGitHubAccessToken();
            }
            case SupportedGitService.AzureDevOps:
                return await getAzDevAccessToken();
            default:
                return null;
        }
    }
}