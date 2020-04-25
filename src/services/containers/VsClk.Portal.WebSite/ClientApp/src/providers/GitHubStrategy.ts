import { SessionData } from 'vscode-web';

import { IAuthStrategy, DEFAULT_GITHUB_VSCODE_AUTH_PROVIDER_ID } from 'vso-workbench';
import { localStorageKeychain, getCurrentEnvironmentId, getVSCodeScheme } from 'vso-client-core';
import { createCascadeTokenKey } from '../split/github/createCascadeTokenKey';
import { getGithubToken } from '../split/github/getGithubToken';

const GH_PR_EXTENSION_STABLE_SERVICE = 'vscode-pull-request-github';
const GH_PR_EXTENSION_STABLE_ACCOUNT = 'github.com';

const githubPrExtensionServices = [
    `${getVSCodeScheme()}-github.login`,
    GH_PR_EXTENSION_STABLE_SERVICE,
];

const githubPrExtensionAccounts = [
    'account',
    GH_PR_EXTENSION_STABLE_ACCOUNT,
];

const isGithubRequest = (service: string, account: string) => {
    const isGithubAccount = githubPrExtensionAccounts.includes(account);
    const isGithubService = githubPrExtensionServices.includes(service);
    const isGithubRequest = (isGithubService && isGithubAccount);

    return isGithubRequest;
};

export class GitHubStrategy implements IAuthStrategy {
    async canHandleService(service: string, account: string) {
        const isVSOGitHubRequest = (service === 'vso-github' &&
            (account.startsWith('github-token_') || account.startsWith('cascade-token_')));

        return isVSOGitHubRequest || isGithubRequest(service, account);
    }

    async getToken(service: string, account: string): Promise<string | null> {
        /**
         * Is either `GitHub PR extension` or `Native VSCode Auth GitHub provider`
         */
        if (isGithubRequest(service, account)) {
            const token = await getGithubToken();
            if (!token) {
                return null;
            }

            /**
             * Old GH PR extension format, before we switched to the native VSCode auth providers
             */
            if (service === GH_PR_EXTENSION_STABLE_SERVICE && account === GH_PR_EXTENSION_STABLE_ACCOUNT) {
                return token;
            }

            /**
             * Native VSCode auth providers format
             *  - `email` scope used byt the `Settings Sync Service`
             *  - `read:user, user:email, repo` scopes used by the new GH PR extension
             */
            const githubSession: SessionData = {
                id: DEFAULT_GITHUB_VSCODE_AUTH_PROVIDER_ID,
                accessToken: token,
                scopes: ['email'],
            };

            const githubSessionPR: SessionData = {
                id: 'github-session-github-pr',
                accessToken: token,
                scopes: ['read:user', 'user:email', 'repo'],
            };

            const githubSessions = JSON.stringify([ githubSession, githubSessionPR ]);
            return githubSessions;
        }

        /**
         * GitHub token for VSO extension.
         */
        if (account.startsWith('github-token_')) {
            const token = await getGithubToken();

            return token;
        }

        /**
         * Cascade token for VSO extension.
         */
        const cascadeKey = createCascadeTokenKey(getCurrentEnvironmentId());
        const token = (await localStorageKeychain.get(cascadeKey)) || null;

        return token;
    }
}
