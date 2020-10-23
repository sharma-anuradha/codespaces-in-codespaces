import { VSCodeDefaultAuthSession } from 'vs-codespaces-authorization';

import { getVSCodeScheme, TSupportedNativeVSCodeAuthProviders } from 'vso-client-core';

import { IAuthStrategy } from '../../../../interfaces/IAuthStrategy';

import { authService } from '../../../../auth/authService';
import { DEFAULT_GITHUB_BROWSER_AUTH_PROVIDER_ID } from '../../../../../src/constants';
import { featureFlags, FeatureFlags } from '../../../../../src/config/featureFlags';

/**
 * The auth strategy that handles the authentication sessions used by
 * the old Native VSCode Authentication providers.
 * (Will be removed later and fully replaced by NewNativeVSCodeProvidersStrategy when VSCode Stable is updated)
 */
export class NativeVSCodeProvidersStrategy implements IAuthStrategy {
    protected getSessions = (
        sessions: VSCodeDefaultAuthSession[],
        type: TSupportedNativeVSCodeAuthProviders
    ): VSCodeDefaultAuthSession[] => {
        const result = sessions.filter((session) => {
            return session.type === type;
        });

        return result;
    };

    protected isService = (service: string, serviceName: TSupportedNativeVSCodeAuthProviders) => {
        return service === `${getVSCodeScheme()}-${serviceName}.login`;
    };

    protected getDefaultSession = async (): Promise<VSCodeDefaultAuthSession[] | null> => {
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

    protected isValidAccountKey(account: string): boolean {
        return account === 'account';
    }

    public async canHandleService(service: string, account: string) {
        // all native providers use `account` for the account argument
        if (!this.isValidAccountKey(account)) {
            return false;
        }

        const sessions = await this.getDefaultSession();
        if (!sessions) {
            return false;
        }

        if (this.isService(service, 'github')) {
            const ghTokens = this.getSessions(sessions, 'github');

            return !!ghTokens.length;
        }

        if (this.isService(service, 'microsoft')) {
            const msTokens = this.getSessions(sessions, 'microsoft');

            return !!msTokens.length;
        }

        return false;
    }

    public async getToken(service: string, account: string): Promise<string | null> {
        if (!this.isValidAccountKey(account)) {
            return null;
        }

        const sessions = await this.getDefaultSession();
        if (!sessions) {
            return null;
        }

        if (this.isService(service, 'github')) {
            const ghTokens = this.getSessions(sessions, 'github');

            // Add session that is required for github-browser extension
            if (ghTokens.length && await featureFlags.isEnabled(FeatureFlags.ServerlessEnabled)) {
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

        if (this.isService(service, 'microsoft')) {
            const msTokens = this.getSessions(sessions, 'microsoft');

            return JSON.stringify(msTokens);
        }

        return null;
    }
}
