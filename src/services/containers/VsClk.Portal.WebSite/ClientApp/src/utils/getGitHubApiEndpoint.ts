import { isHostedOnGithub } from 'vso-client-core';

const defaultApiEndpoint = 'https://api.github.com';
const localApiEndpoint = 'https://api.github.localhost';

const allowedHostnames = [
    'dev.core.vsengsaas.visualstudio.com',
    'workspaces-dev.github.com'
];

const getCurrentHostName = () => {
    const hostname = location.hostname.split('.').slice(1).join('.');

    return hostname;
}

export const getGitHubApiEndpoint = () => {
    if (!isHostedOnGithub()) {
        return defaultApiEndpoint;
    }

    const key = localStorage.getItem('vso-local-github-endpoint');
    if (key === 'true' && allowedHostnames.includes(getCurrentHostName())) {
        return localApiEndpoint;
    }

    return defaultApiEndpoint;
};
