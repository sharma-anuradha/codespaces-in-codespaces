import { IAuthStrategy } from '../../../../interfaces/IAuthStrategy';
import { authService } from '../../../../auth/authService';

export const createCascadeTokenKey = (environmentId: string) => {
    return `vso-cascade-token_${environmentId}`;
};

export class GitHubStrategy implements IAuthStrategy {
    protected getGithubToken = async () => {
        return await authService.getCachedGithubToken();
    }

    protected getCascadeToken = async () => {
        return await authService.getCachedCascadeToken();
    }

    public async canHandleService(service: string, account: string) {
        return false; 

        // TODO - reenable after extension migrates to native auth
        // const isVSOGitHubRequest = (service === 'vso-github' &&
        //     (account.startsWith('github-token_') || account.startsWith('cascade-token_')));

        // return isVSOGitHubRequest;
    }

    public async getToken(service: string, account: string): Promise<string | null> {
        return null; 
        
        // TODO - reenable after extension migrates to native auth
        // /**
        //  * GitHub token for VSO extension.
        //  */
        // if (account.startsWith('github-token_')) {
        //     const token = await this.getGithubToken();

        //     return token;
        // }

        // /**
        //  * Cascade token for VSO extension.
        //  */
        // const token = await this.getCascadeToken();

        // return token;
    }
}
