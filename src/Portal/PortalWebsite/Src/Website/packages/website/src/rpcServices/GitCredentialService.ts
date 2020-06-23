import {
    createTrace,
    isHostedOnGithub,
    getCurrentEnvironmentId,
    localStorageKeychain,
} from 'vso-client-core';

import {
    GitCredentialService as GitCredentialServiceBase,
    SupportedGitService,
} from 'vso-ts-agent';

import { createGitHubTokenKey } from '../split/github/createGitHubTokenKey';
import { getGitHubAccessToken } from '../services/gitHubAuthenticationService';
import { getAzDevAccessToken } from '../services/azDevAuthenticationService';
import { useActionContext } from '../actions/middleware/useActionContext';

export const trace = createTrace('GitCredentialService');

export class GitCredentialService extends GitCredentialServiceBase {
    private isProduction() {
        const { state } = useActionContext();
        const { configuration } = state;

        if (!configuration) {
            throw new Error('No configuration set.');
        }

        return (configuration.environment === 'production');
    }

    private getGithubToken = async () => {
        if (isHostedOnGithub()) {
            const key = createGitHubTokenKey(getCurrentEnvironmentId());
            const token = await localStorageKeychain.get(key);

            if (token) {
                return token;
            }
        }

        return await getGitHubAccessToken();
    }

    public async getTokenByHost(supportedGitService: SupportedGitService, host?: string): Promise<string | null> {
        switch (supportedGitService) {
            case SupportedGitService.GitHub: {
                return await this.getGithubToken();
            }
            case SupportedGitService.AzureDevOps: {
                return await getAzDevAccessToken();
            }
            case SupportedGitService.Unknown: {
                // disallow unknown services in PROD
                if (this.isProduction() || !host) {
                    return null;
                }
                /**
                 * For development purposes we return GitHub token
                 * for the git fill request with `ngrok` host.
                 */
                if (host.match(/ghdev\-.+\.ngrok\.io/)) {
                    return await this.getGithubToken();
                }

                return null;
            }
            default: {
                return null;
            }
        }
    }
}
