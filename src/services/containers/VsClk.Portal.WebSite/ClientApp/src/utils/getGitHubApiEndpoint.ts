import { isHostedOnGithub } from 'vso-client-core';

const defaultApiEndpoint = 'https://api.github.com';
const localApiEndpoint = 'https://api.github.localhost';

export const getGitHubApiEndpoint = () => {
    if (!isHostedOnGithub()) {
        return defaultApiEndpoint;
    }

    const key = localStorage.getItem('vso-local-github-endpoint');
    if (key === 'true') {
        return localApiEndpoint;
    }

    return defaultApiEndpoint;
};
