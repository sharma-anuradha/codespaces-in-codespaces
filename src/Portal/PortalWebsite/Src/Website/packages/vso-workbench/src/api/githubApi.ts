import { getGitHubApiEndpoint } from '../utils/getGithubApiEndpoint';

export class GitHubApi {
    static async getHeadOfPullRequest(
        org: string,
        repository: string,
        pullNumber: string,
        githubToken: null | string
    ) {
        const headers = new Headers();
        headers.set('Content-Type', 'application/json');
        if (githubToken) {
            headers.set('Authorization', `Bearer ${githubToken}`);
        }
        const fetchParams: RequestInit = {
            method: 'GET',
            headers,
        };

        const githubApiEndpoint = await getGitHubApiEndpoint(`/repos/${org}/${repository}/pulls/${pullNumber}`);
        const result = await fetch(
            githubApiEndpoint,
            fetchParams
        );

        if (result.status !== 200) {
            return '';
        }

        return (await result.json()).head.sha as string;
    }
}
