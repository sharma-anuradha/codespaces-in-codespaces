import { IAuthStrategy } from '../../../../interfaces/IAuthStrategy';

export class GitHubStrategy implements IAuthStrategy {
    async canHandleService(service: string, account: string) {
        return (
            service === 'vso-github' &&
            (account.startsWith('github-token_') || account.startsWith('cascade-token_'))
        );
    }

    async getToken(service: string, account: string): Promise<string | null> {
        throw new Error('AzureAccountStrategy not implemented.');
        // if (account.startsWith('github-token_')) {
        //     return await getStoredGitHubToken();
        // }
        // return await localStorageKeychain.get(`vso-${account}`) || null;
    }
}
