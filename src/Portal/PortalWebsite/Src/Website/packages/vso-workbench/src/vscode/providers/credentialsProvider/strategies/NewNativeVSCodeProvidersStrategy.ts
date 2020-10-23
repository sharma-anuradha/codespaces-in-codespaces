import { getVSCodeScheme, TSupportedNativeVSCodeAuthProviders } from 'vso-client-core';
import { NativeVSCodeProvidersStrategy } from './NativeVSCodeProvidersStrategy';

const GH_ACCOUNT_KEY = 'github.auth';
const MS_ACCOUNT_KEY = 'microsoft.login';

const GH_AUTH_EXTENSION_ID = 'vscode.github-authentication';
const MS_AUTH_EXTENSION_ID = 'vscode.microsoft-authentication';

/**
 * The auth strategy that handles the authentication sessions used by
 * the Native VSCode Authentication providers.
 */
export class NewNativeVSCodeProvidersStrategy extends NativeVSCodeProvidersStrategy {
    private buildServiceKey = (serviceName: TSupportedNativeVSCodeAuthProviders) => {
        return `${getVSCodeScheme()}vscode.${serviceName}-authentication`;
    }

    protected isService = (service: string, serviceName: TSupportedNativeVSCodeAuthProviders) => {
        switch (serviceName) {
            case 'github':
            case 'microsoft': {
                return service === this.buildServiceKey(serviceName);
            }

            default: {
                return false;
            }
        }
    };

    protected isValidAccountKey(account: string): boolean {
        return [
            GH_ACCOUNT_KEY,
            MS_ACCOUNT_KEY,
        ].includes(account);
    }

    public async getToken(service: string, account: string): Promise<string | null> {
        const defaultAuthSessions = await super.getToken(service, account);
        if (!defaultAuthSessions) {
            return null;
        }

        if (this.isService(service, 'github')) {
            return JSON.stringify({
                extensionId: GH_AUTH_EXTENSION_ID,
                content: defaultAuthSessions,
            });
        }

        if (this.isService(service, 'microsoft')) {
            return JSON.stringify({
                extensionId: MS_AUTH_EXTENSION_ID,
                content: defaultAuthSessions,
            });
        }

        return null;
    }
}
