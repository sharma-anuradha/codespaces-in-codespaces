import {
    createTrace,
    maybePii,
    isHostedOnGithub,
    getCurrentEnvironmentId,
    localStorageKeychain
} from 'vso-client-core';

import {
    GitCredentialService as GitCredentialServiceBase,
    GitCredentialsRequest,
} from 'vso-ts-agent';

import { SupportedGitService, getSupportedGitServiceByHost } from '../utils/gitUrlNormalization';
import { getGitHubAccessToken } from '../services/gitHubAuthenticationService';
import { getAzDevAccessToken } from '../services/azDevAuthenticationService';
import { createGitHubTokenKey } from '../split/github/createGitHubTokenKey';

export const trace = createTrace('GitCredentialService');

export class GitCredentialService extends GitCredentialServiceBase {
    public async onRequest([input]: string[]) {
        const fillRequest = parseGitCredentialsFillInput(input);

        trace.verbose('Received git credential fill request', maybePii(fillRequest));

        if (fillRequest.protocol === 'https' || fillRequest.protocol === 'http') {
            trace.verbose('Resolving ' + fillRequest.host + ' credential.');

            const token = await this.getTokenByHost(
                getSupportedGitServiceByHost(fillRequest.host)
            );

            if (token) {
                trace.verbose('Filled credential.', maybePii(fillRequest));

                return `username=${token}\npassword=x-oauth-basic\n`;
            }
        }

        trace.verbose('Failed to fill credential.', maybePii(fillRequest));

        return input;
    }

    private async getTokenByHost(supportedGitService: SupportedGitService): Promise<string | null> {
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

function parseGitCredentialsFillInput(str: string): GitCredentialsRequest {
    // Git asks for credentials in form of string request
    // E.g:
    //      protocol=https
    //      host=github.com
    //
    // https://git-scm.com/docs/git-credential#IOFMT
    //

    const lines = str.split('\n');
    let result: GitCredentialsRequest = {};
    return lines.reduce((parsedInput: any, line) => {
        const [key, value] = getKeyValuePair(line);

        if (key) {
            parsedInput[key] = value;
        }

        return parsedInput;
    }, result);

    function getKeyValuePair(line: string) {
        const delimiterIndex = line.indexOf('=');
        if (delimiterIndex <= 0) {
            return [];
        }
        return [line.slice(0, delimiterIndex), line.slice(delimiterIndex + 1)];
    }
}
