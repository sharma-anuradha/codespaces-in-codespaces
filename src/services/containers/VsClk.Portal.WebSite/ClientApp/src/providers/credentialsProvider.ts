import { ICredentialsProvider } from 'vscode-web';

import { createTrace } from '../utils/createTrace';
import { authService } from '../services/authService';
import { getStoredGitHubToken } from '../services/gitHubAuthenticationService';
import { localStorageKeychain } from '../cache/localStorageKeychainInstance';

const trace = createTrace('credentials-provider:info');

interface IAuthStrategy {
    canHandleService(service: string, account: string): boolean;

    getToken(service: string, account: string): Promise<string | null>;
}

class MsalAuthStrategy implements IAuthStrategy {
    canHandleService(service: string, account: string): boolean {
        const isVSCodeAccount = service === 'VS Code Account';
        const isAADv2AccessToken = account === 'AADv2.accessToken';

        return isVSCodeAccount && isAADv2AccessToken;
    }

    async getToken(service: string, account: string): Promise<string | null> {
        const token = await authService.getCachedToken();

        if (!token) {
            return null;
        }

        return token.accessToken;
    }
}

class AADv2BrowserSyncStrategy implements IAuthStrategy {
    canHandleService(service: string, account: string): boolean {
        return service === 'aadv2browsersync_vscode-account' || account === 'AADv2BrowserSync';
    }

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
    canHandleService(service: string, account: string): boolean {
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

class LiveShareWebStrategy implements IAuthStrategy {
    canHandleService(service: string, account: string): boolean {
        return service === 'liveshare-web' && account === 'accesstoken';
    }

    async getToken(service: string, account: string): Promise<string | null> {
        const token = await authService.getCachedToken();

        if (!token) {
            return null;
        }

        return token.accessToken;
    }
}

class GistPadStrategy implements IAuthStrategy {
    canHandleService(service: string, account: string): boolean {
        return service === 'vscode-gistpad' && account === 'gist-token';
    }

    async getToken(service: string, account: string): Promise<string | null> {
        return getStoredGitHubToken('gist');
    }
}

class GitHubStrategy implements IAuthStrategy {
    canHandleService(service: string, account: string): boolean {
        return service === 'vso-github' && (
            account.startsWith('github-token_') ||
            account.startsWith('cascade-token_')
        );
    }

    async getToken(service: string, account: string): Promise<string | null> {
        return await localStorageKeychain.get(`vso-${account}`) || null;
    }
}

const GENERIC_PREFIX = 'vsonline.keytar';

export class CredentialsProvider implements ICredentialsProvider {
    constructor(private strategies: IAuthStrategy[]) {}

    private generateGenericLocalStorageKey(service: string, account: string) {
        return `${GENERIC_PREFIX}.${service}.${account}`;
    }

    async getPassword(service: string, account: string): Promise<string | null> {
        trace.verbose('Responding to VSCode keytar-shim request.', { service, account });

        const strategy = this.strategies.find((strategy) =>
            strategy.canHandleService(service, account)
        );

        if (!strategy) {
            trace.verbose('Cannot respond to VSCode keytar-shim request.', { service, account });

            return null;
        }

        const token = await strategy.getToken(service, account);

        if (token) {
            return token;
        }

        // generic keytar request
        const genericKey = this.generateGenericLocalStorageKey(service, account);
        const password = await localStorageKeychain.get(genericKey);

        if (password) {
            return password;
        }

        trace.warn('No token available.');

        return null;
    }

    async setPassword(service: string, account: string, password: string): Promise<void> {
        const key = this.generateGenericLocalStorageKey(service, account);
        await localStorageKeychain.set(key, password);
    }

    async deletePassword(service: string, account: string): Promise<boolean> {
        const key = this.generateGenericLocalStorageKey(service, account);
        const isPresent = localStorageKeychain.has(key);

        await localStorageKeychain.delete(key);

        return isPresent;
    }

    findPassword(service: string): Promise<string | null> {
        return Promise.resolve(null);
    }

    findCredentials(service: string): Promise<{ account: string; password: string }[]> {
        return Promise.resolve([]);
    }
}

export const credentialsProvider = new CredentialsProvider([
    new AADv2BrowserSyncStrategy(),
    new MsalAuthStrategy(),
    new AzureAccountStrategy(),
    new LiveShareWebStrategy(),
    new GistPadStrategy(),
    new GitHubStrategy(),
]);
