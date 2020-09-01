import { VSCodeDefaultAuthSession } from 'vs-codespaces-authorization';

import { getVSCodeScheme } from 'vso-client-core';

import { IAuthStrategy } from '../../../../interfaces/IAuthStrategy';

import { authService } from '../../../../auth/authService';
import { TSupportedNativeVSCodeAuthProviders } from '../../../../../../vso-client-core/src/interfaces/IPartnerInfo';
import { DEFAULT_GITHUB_BROWSER_AUTH_PROVIDER_ID } from '../../../../../src/constants';
import { featureFlags, FeatureFlags } from '../../../../../src/config/featureFlags';

/**
 * The auth strategy that handles the authentication sessions used by
 * the Native VSCode Authentication providers.
 */
export class NativeVSCodeProvidersStrategy implements IAuthStrategy {
    private getSessions = (
        sessions: VSCodeDefaultAuthSession[],
        type: TSupportedNativeVSCodeAuthProviders
    ): VSCodeDefaultAuthSession[] => {
        const result = sessions.filter((session) => {
            return session.type === type;
        });

        return result;
    };

    private isGitHubService = (service: string) => {
        return service === `${getVSCodeScheme()}-github.login`;
    };

    private isMicrosoftService = (service: string) => {
        return service === `${getVSCodeScheme()}-microsoft.login`;
    };

    private getDefaultSession = async (): Promise<VSCodeDefaultAuthSession[] | null> => {
        const info = await authService.getPartnerInfo();
        if (!info) {
            throw new Error('Cannot get partner info.');
        }

        if (!('vscodeSettings' in info)) {
            throw new Error('No `vscodeSettings` is set on payload.');
        }

        // no sessions set
        if (!info || !info.vscodeSettings.defaultAuthSessions?.length) {
            return null;
        }

        const { defaultAuthSessions } = info.vscodeSettings;

        return defaultAuthSessions;
    };

    public async canHandleService(service: string, account: string) {
        // all native providers use `account` for the account argument
        if (account !== 'account') {
            return false;
        }

        const sessions = await this.getDefaultSession();
        if (!sessions) {
            return false;
        }

        if (this.isGitHubService(service)) {
            const ghTokens = this.getSessions(sessions, 'github');

            return !!ghTokens.length;
        }

        if (this.isMicrosoftService(service)) {
            const msTokens = this.getSessions(sessions, 'microsoft');

            return !!msTokens.length;
        }

        return false;
    }

    public async getToken(service: string, account: string): Promise<string | null> {
        const sessions = await this.getDefaultSession();
        if (!sessions) {
            return null;
        }

        if (this.isGitHubService(service)) {
            const ghTokens = this.getSessions(sessions, 'github');

            // Add session that is required for github-browser extension
            if (await featureFlags.isEnabled(FeatureFlags.ServerlessEnabled) && ghTokens.length > 0) {
                const githubSessionRepo: VSCodeDefaultAuthSession = {
                    id: DEFAULT_GITHUB_BROWSER_AUTH_PROVIDER_ID,
                    accessToken: ghTokens[0].accessToken,
                    scopes: ['repo'],
                    type: 'github'
                };
                ghTokens.push(githubSessionRepo);
            }

            return JSON.stringify(ghTokens);
        }

        if (this.isMicrosoftService(service)) {
            const msTokens = this.getSessions(sessions, 'microsoft');

            return JSON.stringify(msTokens);
        }

        return null;
    }
}
