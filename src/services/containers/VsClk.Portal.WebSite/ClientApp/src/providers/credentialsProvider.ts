import { ICredentialsProvider } from 'vscode-web';

import { createTrace } from '../utils/createTrace';
import { authService } from '../services/authService';
import { localStorageKeyVault } from '../cache/localStorageKeyVaultInstance';

const trace = createTrace('credentials-provider:info');

type VSCodeAccountIToken = {
    accessToken: string;
    expiresOn: number;
};

interface IAuthStrategy {
    canHandleService(service: string, account: string): boolean;

    getToken(service: string, account: string): Promise<string | null>;
}

class MsalAuthStrategy implements IAuthStrategy {
    canHandleService(service: string, account: string): boolean {
        return service === 'VS Code Account' && account === 'AAD';
    }

    async getToken(service: string, account: string): Promise<string | null> {
        // TODO: write one that relies on store
        const token = await authService.getCachedToken();

        if (!token) {
            return null;
        }

        const response: VSCodeAccountIToken = {
            accessToken: token.accessToken,
            expiresOn: token.expiresOn.getTime(),
        };

        return JSON.stringify(response);
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
        return ((service === 'VS Code Azure') && (account === 'Azure'));
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

const GENERIC_PREFIX = 'vsonline.keytar';

export class CredentialsProvider implements ICredentialsProvider {
    constructor(private strategies: IAuthStrategy[]) {}

    private generateGenericLocalStorageKey(service: string, account: string) {
        return `${GENERIC_PREFIX}.${service}.${account}`;
    }

    async getPassword(service: string, account: string): Promise<string | null> {
        trace.verbose('Responding to VSCode keytar-shim request.', { service, account });

        // generic keytar request
        const genericKey = this.generateGenericLocalStorageKey(service, account);
        const password = await localStorageKeyVault.get(genericKey);
        if (password) {
            return password;
        }

        const strategy = this.strategies.find((strategy) =>
            strategy.canHandleService(service, account)
        );

        if (!strategy) {
            trace.verbose('Cannot respond to VSCode keytar-shim request.', { service, account });

            return null;
        }

        const token = await strategy.getToken(service, account);

        if (!token) {
            trace.warn('No token available.');
        }

        return token;
    }

    async setPassword(service: string, account: string, password: string): Promise<void> {
        const key = this.generateGenericLocalStorageKey(service, account);
        await localStorageKeyVault.set(key, password);
    }

    async deletePassword(service: string, account: string): Promise<boolean> {
        const key = this.generateGenericLocalStorageKey(service, account);
        const isPresent = localStorageKeyVault.has(key);

        await localStorageKeyVault.delete(key);

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
    new AzureAccountStrategy()
]);
