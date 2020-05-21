import {
    getVSCodeScheme,
} from 'vso-client-core';

import { IAuthStrategy } from '../../../../interfaces/IAuthStrategy';

import { authService } from '../../../../auth/authService';
import {
    INativeAuthProviderSession,
    TSupportedNativeVSCodeAuthProviders,
} from '../../../../../../vso-client-core/src/interfaces/IPartnerInfo';

/**
 * The auth strategy that handles the authentication sessions used by
 * the Native VSCode Authentication providers.
 */
export class NativeVSCodeProvidersStrategy implements IAuthStrategy {
    private getSessions = (
        sessions: INativeAuthProviderSession[],
        type: TSupportedNativeVSCodeAuthProviders
    ): INativeAuthProviderSession[] => {
        const result = sessions.filter((session) => {
            return session.type === type;
        });

        return result;
    };

    private isGitHubService = (service: string)=> {
        return (service === `${getVSCodeScheme()}-github.login`);
    };

    private isMicrosoftService = (service: string) => {
        return (service === `${getVSCodeScheme()}-microsoft.login`);
    };

    private getDefaultSession = async (): Promise<INativeAuthProviderSession[] | null> => {
        const info = await authService.getPartnerInfo();
        if (!info) {
            throw new Error('Cannot get partner info.');
        }

        if (!('cascadeToken' in info)) {
            throw new Error('The old payload provided or no `cascadeToken` set.');
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

            return JSON.stringify(ghTokens);
        }

        if (this.isMicrosoftService(service)) {
            const msTokens = this.getSessions(sessions, 'microsoft');

            return JSON.stringify(msTokens);
        }

        return null;
    }
}
