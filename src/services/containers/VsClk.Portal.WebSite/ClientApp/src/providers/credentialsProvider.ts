import {
    CredentialsProvider,
    IAuthStrategy,
    MsalAuthStrategy as MsalAuthStrategyWorkbench,
    AADv2BrowserSyncStrategy as AADv2BrowserSyncStrategyWorkbench,
    LiveShareWebStrategy as LiveShareWebStrategyWorkbench,
    LiveShareGithubAuthStrategy as LiveShareGithubAuthStrategyWorkbench,
} from 'vso-workbench';

import {
    localStorageKeychain,
    getCurrentEnvironmentId,
    isHostedOnGithub} from 'vso-client-core';

import { authService } from '../services/authService';
import { getStoredGitHubToken } from '../services/gitHubAuthenticationService';
import { getStoredAzDevToken } from '../services/azDevAuthenticationService';
import { createCascadeTokenKey } from '../split/github/createCascadeTokenKey';
import { GitHubStrategy } from './GitHubStrategy';

class MsalAuthStrategy extends MsalAuthStrategyWorkbench {
    async getToken(service: string, account: string): Promise<string | null> {
        const token = await authService.getCachedToken();

        if (!token) {
            return null;
        }

        return token.accessToken;
    }
}

class AADv2BrowserSyncStrategy extends AADv2BrowserSyncStrategyWorkbench {
    async getToken(service: string, account: string): Promise<string | null> {
        // TODO: write one that relies on store
        const token = await authService.getCachedToken();

        if (!token) {
            return null;
        }

        return token.accessToken;
    }
}

/**
 * A temporary spolution for Azure Acccount for ignite.
 * We should move to `refreshToken`-based solution in the nearest future.
 */
class AzureAccountStrategy implements IAuthStrategy {
    async canHandleService(service: string, account: string) {
        return service === 'VS Code Azure' && account === 'Azure';
    }

    async getToken(): Promise<string | null> {
        const authCode = await authService.getAuthCode();

        if (!authCode) {
            return null;
        }

        try {
            return JSON.stringify(authCode);
        } catch {
            // ignore
            return null;
        }
    }
}

class LiveShareWebStrategy extends LiveShareWebStrategyWorkbench {
    async getToken(service: string, account: string): Promise<string | null> {
        const token = await authService.getCachedToken();

        if (!token) {
            return null;
        }

        return token.accessToken;
    }
}

class GistPadStrategy implements IAuthStrategy {
    async canHandleService(service: string, account: string) {
        return service === 'vscode-gistpad' && account === 'gist-token';
    }

    async getToken(service: string, account: string): Promise<string | null> {
        return getStoredGitHubToken('gist');
    }
}

class AzureDevOpsStrategy implements IAuthStrategy {
    async canHandleService(service: string, account: string) {
        return service === 'vscode-azdev' && account === 'accesstoken';
    }

    async getToken(service: string, account: string): Promise<string | null> {
        return getStoredAzDevToken();
    }
}

class LiveShareGithubAuthStrategy extends LiveShareGithubAuthStrategyWorkbench {
    async getToken(service: string, account: string): Promise<string | null> {
        const key = createCascadeTokenKey(getCurrentEnvironmentId());

        const token = (await localStorageKeychain.get(key)) || null;

        return token;
    }
}

const getProviders = () => {
    return (isHostedOnGithub())
        ? [
            new GitHubStrategy(),
            new LiveShareGithubAuthStrategy()
        ]
        : [
            new AADv2BrowserSyncStrategy(),
            new MsalAuthStrategy(),
            new AzureAccountStrategy(),
            new LiveShareWebStrategy(),
            new GistPadStrategy(),
            new AzureDevOpsStrategy(),
        ];
};

export const credentialsProvider = new CredentialsProvider(getProviders());
