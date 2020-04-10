import { authService } from '../../../../auth/authService';
import { IAuthStrategy } from '../../../../interfaces/IAuthStrategy';

export class MsalAuthStrategy implements IAuthStrategy {
    async canHandleService(service: string, account: string) {
        const isVSCodeAccount = service === 'VS Code Account';
        const isAADv2AccessToken = account === 'AADv2.accessToken';
        return isVSCodeAccount && isAADv2AccessToken;
    }
    async getToken(service: string, account: string): Promise<string | null> {
        const token = await authService.getCachedToken();

        if (!token) {
            return null;
        }

        return token;
    }
}
