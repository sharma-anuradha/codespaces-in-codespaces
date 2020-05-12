import { IAuthStrategy } from '../../../../interfaces/IAuthStrategy';

export class GistPadStrategy implements IAuthStrategy {
    async canHandleService(service: string, account: string) {
        return service === 'vscode-gistpad' && account === 'gist-token';
    }

    async getToken(service: string, account: string): Promise<string | null> {
        throw new Error('AzureAccountStrategy not implemented.');
        // return getStoredGitHubToken('gist');
    }
}
