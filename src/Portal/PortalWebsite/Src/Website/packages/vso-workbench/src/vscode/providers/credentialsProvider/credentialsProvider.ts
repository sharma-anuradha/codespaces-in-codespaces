import { ICredentialsProvider } from 'vscode-web';
import { createTrace, localStorageKeychain } from 'vso-client-core';

import { IAuthStrategy } from '../../../interfaces/IAuthStrategy';
import { MsalAuthStrategy } from './strategies/MsalAuthStrategy';
import { AADv2BrowserSyncStrategy } from './strategies/AADv2BrowserSyncStrategy';
import { LiveShareWebStrategy } from './strategies/LiveShareWebStrategy';
import { GitCredentialHelperStrategy } from './strategies/GitServiceCredentialsStrategy';
import { LiveShareGithubAuthStrategy } from './strategies/CascadeAuthStrategy';
import { GitHubStrategy } from './strategies/GitHubStrategy';
import { NativeVSCodeProvidersStrategy } from './strategies/NativeVSCodeProvidersStrategy';

const trace = createTrace('credentials-provider:info');

const GENERIC_PREFIX = 'vsonline.keytar';

export class CredentialsProvider implements ICredentialsProvider {
    constructor(private strategies: IAuthStrategy[]) {}

    private generateGenericLocalStorageKey(service: string, account: string) {
        return `${GENERIC_PREFIX}.${service}.${account}`;
    }

    async getPassword(service: string, account: string): Promise<string | null> {
        trace.verbose('Responding to VSCode keytar-shim request.', { service, account });

        /**
         * Check if any strategy can handle the request
         */
        for (let strategy of this.strategies) {
            const isCanHandle = await strategy.canHandleService(service, account);
            if (!isCanHandle) {
                continue;
            }

            const token = await strategy.getToken(service, account);
            if (token) {
                return token;
            }
        }

        /**
         * If no strategy can handle the request, look for a generic key next.
         */

        trace.verbose('Cannot respond to VSCode keytar-shim request.', { service, account });

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
    // Strategy for the LS GitHub (Cascade token)
    new LiveShareGithubAuthStrategy(),
    /**
     * Used only by the vscode-account extension.
     * can be removed when the extension uses
     * `VS Code Account.AADv2.accessToken`
     * key from the MSAL strategy below
     */
    new AADv2BrowserSyncStrategy(),
    /**
     * Default `vscode-account` strategy.
     */
    new MsalAuthStrategy(),
    /**
     * Default `LiveShareWeb` strategy.
     */
    new LiveShareWebStrategy(),
    /**
     * Partners can pass list of git credentials
     * with the authentication requests, the strategy
     * immitates the Git Credentials Helper on top
     * of the Keytar to pass those credentials to the
     * extensions:
     *  - Keytar[service] -> GCH[host]
     *  - Keytar[account | *] -> GCH[path]
     */
    new GitCredentialHelperStrategy(),
    /**
     * Used to authente:
     *  - VSCS extension
     *  - GHPR extension (old one thru the keytar request directly)
     *  - The native GitHub auth provider (GHPR extension uses it to auth)
     */
    new GitHubStrategy(),
    /**
     * Used to add the default authentication sessions used by the Native VSCode
     * authentication providers, these data is coming from
     * the ICrossDomainPartnerInfo.vscodeSettings.defaultAuthSessions payload (optional).
     */
    new NativeVSCodeProvidersStrategy(),
]);
