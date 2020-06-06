import {
    DEFAULT_GITHUB_VSCODE_AUTH_PROVIDER_ID, IAuthStrategy,
} from 'vso-workbench';
import {
    localStorageKeychain,
    getCurrentEnvironmentId,
    getVSCodeScheme,
} from 'vso-client-core';

import { createCascadeTokenKey } from '../split/github/createCascadeTokenKey';
import { getGithubToken } from '../split/github/getGithubToken';
import { SessionData } from 'vscode-web';

const GH_PR_EXTENSION_STABLE_SERVICE = 'vscode-pull-request-github';
const GH_PR_EXTENSION_STABLE_ACCOUNT = 'github.com';

const githubKnownServices = [
    `${getVSCodeScheme()}-github.login`,
    GH_PR_EXTENSION_STABLE_SERVICE,
];

const githubKnownAccounts = [
    'account',
    GH_PR_EXTENSION_STABLE_ACCOUNT,
];

const isGithubRequest = (service: string, account: string) => {
    const isGithubAccount = githubKnownAccounts.includes(account);
    const isGithubService = githubKnownServices.includes(service);
    const isGithubRequest = (isGithubService && isGithubAccount);

    return isGithubRequest;
};

export class GitHubStrategy implements IAuthStrategy {
    protected getGithubToken = async () => {
        return await getGithubToken();
    }

    protected getCascadeToken = async () => {
        const key = createCascadeTokenKey(getCurrentEnvironmentId());
        const token = await localStorageKeychain.get(key);

        return token || null;
    }

    public async canHandleService(service: string, account: string) {
        return isGithubRequest(service, account) || (
            service === 'vso-github' &&
            (account.startsWith('github-token_') || account.startsWith('cascade-token_'))
        );
    }

    public async getToken(service: string, account: string) {
        /**
         * Is either `GitHub PR extension` or `Native VSCode Auth GitHub provider`
         */
        if (isGithubRequest(service, account)) {
            const token = await this.getGithubToken();
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
                scopes: ['read:user', 'user:email', 'repo'].sort(),
            };

            const githubSessionVSCS: SessionData = {
                id: 'github-session-vs-codespaces',
                accessToken: token,
                scopes: ['read:user', 'user:email', 'repo', 'write:discussion'].sort(),
            };

            const githubSessions = JSON.stringify([
                githubSession,
                githubSessionPR,
                githubSessionVSCS,
            ]);

            return githubSessions;
        }

        // Fallback for old VSCS extensions using GitHubBrowserAuthentication
        if (account.startsWith('github-token_')) {
            const token = await this.getGithubToken();
            return token;
        }

        if (account.startsWith('cascade-token_')) {
            const token = await this.getCascadeToken();
            return token;
        }

        return null;
    }
}
