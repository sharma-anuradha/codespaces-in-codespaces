export const createGitHubTokenKey = (environmentId: string) => {
    return `vso-github-token_${environmentId}`;
};
