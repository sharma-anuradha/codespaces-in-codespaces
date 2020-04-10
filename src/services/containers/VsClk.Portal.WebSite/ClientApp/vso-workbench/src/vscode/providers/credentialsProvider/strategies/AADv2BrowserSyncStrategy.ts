import { authService } from '../../../../auth/authService';
import { IAuthStrategy } from '../../../../interfaces/IAuthStrategy';

export class AADv2BrowserSyncStrategy implements IAuthStrategy {
    async canHandleService(service: string, account: string) {
        return service === 'aadv2browsersync_vscode-account' || account === 'AADv2BrowserSync';
    }

    async getToken(service: string, account: string): Promise<string | null> {
        // TODO: write one that relies on store
        const token = await authService.getCachedToken();

        if (!token) {
            return null;
        }

        return token;
    }
}
