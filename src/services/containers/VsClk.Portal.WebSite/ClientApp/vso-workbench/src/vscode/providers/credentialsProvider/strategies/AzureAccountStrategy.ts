import { IAuthStrategy } from '../../../../interfaces/IAuthStrategy';
/**
 * A temporary spolution for Azure Acccount for ignite.
 * We should move to `refreshToken`-based solution in the nearest future.
 */
export class AzureAccountStrategy implements IAuthStrategy {
    async canHandleService(service: string, account: string) {
        return service === 'VS Code Azure' && account === 'Azure';
    }

    async getToken(): Promise<string | null> {
        throw new Error('AzureAccountStrategy not implemented.');
        // const authCode = await authService.getAuthCode();
        // if (!authCode) {
        //     return null;
        // }
        // try {
        //     return JSON.stringify(authCode);
        // }
        // catch {
        //     // ignore
        //     return null;
        // }
    }
}
