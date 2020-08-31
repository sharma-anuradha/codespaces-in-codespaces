import { IAuthStrategy } from '../../../../interfaces/IAuthStrategy';
import { authService } from '../../../../auth/authService';
import { DEFAULT_GITHUB_BROWSER_AUTH_PROVIDER_ID } from '../../../../constants';
import { getVSCodeScheme } from 'vso-client-core';
import { SessionData } from 'vscode-web';

export const createCascadeTokenKey = (environmentId: string) => {
    return `vso-cascade-token_${environmentId}`;
};

export class GitHubStrategy implements IAuthStrategy {
    protected getGithubToken = async () => {
        return await authService.getCachedGithubToken();
    };

    protected getCascadeToken = async () => {
        return await authService.getCachedCodespaceToken();
    };

    public async canHandleService(service: string, account: string) {
        return service === `${getVSCodeScheme()}-github.login`;

        // TODO - reenable after extension migrates to native auth
        // const isVSOGitHubRequest = (service === 'vso-github' &&
        //     (account.startsWith('github-token_') || account.startsWith('cascade-token_')));

        // return isVSOGitHubRequest;
    }

    public async getToken(service: string, account: string): Promise<string | null> {
        if (service !== `${getVSCodeScheme()}-github.login`) {
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

        
        const token = await this.getGithubToken();
        if (!token) {
            return null;
        }
        const githubSessionRepo: SessionData = {
            id: DEFAULT_GITHUB_BROWSER_AUTH_PROVIDER_ID,
            accessToken: token,
            scopes: ['repo'],
        };
        const githubSessions = JSON.stringify([
            githubSessionRepo,
        ]);
        return githubSessions;
    }
}
