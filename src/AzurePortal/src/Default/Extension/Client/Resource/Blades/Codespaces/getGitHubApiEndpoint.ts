const defaultApiEndpoint = 'https://api.github.com';
const localApiEndpoint = 'https://api.github.localhost';

const allowedHostnames = [
    'dev.core.vsengsaas.visualstudio.com',
    'workspaces-dev.github.com',
    'local.github.dev',
];

const getCurrentHostName = () => {
    const hostname = location.hostname.split('.').slice(1).join('.');

    return hostname;
}

export const getGitHubApiEndpoint = () => {
    const key = localStorage.getItem('vso-local-github-endpoint');
    if (key === 'true' && allowedHostnames.includes(getCurrentHostName())) {
        return localApiEndpoint;
    }

    return defaultApiEndpoint;
};
