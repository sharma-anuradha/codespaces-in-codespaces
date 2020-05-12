import { authService } from '../../../../auth/authService';
import { IAuthStrategy } from '../../../../interfaces/IAuthStrategy';

export class LiveShareWebStrategy implements IAuthStrategy {
    async canHandleService(service: string, account: string) {
        return service === 'liveshare-web' && account === 'accesstoken';
    }

    async getToken(service: string, account: string): Promise<string | null> {
        const token = await authService.getCachedToken();

        if (!token) {
            return null;
        }
        return token;
    }
}
